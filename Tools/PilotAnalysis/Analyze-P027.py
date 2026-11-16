"""Local, deterministic n=1 exploration. Never edits recordings, annotations or Unity.
Only NumPy/Pandas and the Python standard library are required. No network calls.
"""
import argparse, csv, hashlib, html, json, math, shutil, sys
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
import numpy as np
import pandas as pd

SEED=16112026
FEATURES=['pupil_region_centered_mm','pupil_iqr_mm','head_log1p_median_deg_s','target_contact_fraction','target_switches_per_s','grab_starts_per_s','wrong_insertion_verdicts_per_s','right_insertion_verdicts_per_s','board_contact_fraction']

def js(p):return json.loads(Path(p).read_text(encoding='utf-8-sig'))
def sha(p):
    h=hashlib.sha256()
    with Path(p).open('rb') as f:
        for b in iter(lambda:f.read(2**20),b''):h.update(b)
    return h.hexdigest()
def clean_json(x):
    if isinstance(x,dict):return {str(k):clean_json(v) for k,v in x.items()}
    if isinstance(x,(list,tuple)):return [clean_json(v) for v in x]
    if isinstance(x,np.ndarray):return clean_json(x.tolist())
    if isinstance(x,(np.integer,)):return int(x)
    if isinstance(x,(np.floating,float)):return float(x) if np.isfinite(x) else None
    if isinstance(x,np.bool_):return bool(x)
    return x
def dump(p,x):Path(p).write_text(json.dumps(clean_json(x),indent=2,allow_nan=False),encoding='utf-8')
def number(s):return pd.to_numeric(s.astype(str).str.replace(r"^'(?=[+\-.0-9])",'',regex=True),errors='coerce').to_numpy(float)
def yes(s):return s.astype(str).str.casefold().eq('true').to_numpy()
def median(v):v=np.asarray(v);v=v[np.isfinite(v)];return float(np.median(v)) if len(v) else np.nan
def iqr(v):v=np.asarray(v);v=v[np.isfinite(v)];return float(np.quantile(v,.75)-np.quantile(v,.25)) if len(v) else np.nan
def overlaps(a,b,c,d):return max(a,c)<min(b,d)
def clock(t):return f'{int(t//60):02d}:{t%60:06.3f}'
def write_csv(p,records,columns=None):
    records=clean_json(records)
    if columns is None:columns=list(dict.fromkeys(k for r in records for k in r))
    with Path(p).open('w',encoding='utf-8',newline='') as f:
        w=csv.DictWriter(f,fieldnames=columns,lineterminator='\n');w.writeheader()
        for r in records:w.writerow({k:json.dumps(v,ensure_ascii=False) if isinstance(v,(dict,list)) else v for k,v in r.items()})

class PairCoverage:
    """Consecutive source-record pairs only; query borrows no out-of-interval endpoint."""
    def __init__(self,t,valid,keys=None):
        dt=np.diff(t);ok=valid[:-1]&valid[1:]&np.isfinite(dt)&(dt>0)&(dt<=.1)
        if keys is not None:ok &= keys[:-1]==keys[1:]
        self.a=t[:-1][ok];self.b=t[1:][ok];self.sum=np.r_[0,np.cumsum(dt[ok])]
        assert np.all(np.diff(self.a)>=0) and np.all(np.diff(self.b)>=0)
    def at(self,a,b):
        i=np.searchsorted(self.a,a,'left');j=np.searchsorted(self.b,b,'left')
        return float(self.sum[j]-self.sum[i]) if j>i else 0.

def kmeans(X,k,seed=SEED,restarts=16):
    """K-means++ initialization and Lloyd updates; retain minimum-SSE nonempty run."""
    n=len(X);best=None
    for restart in range(restarts):
        rng=np.random.default_rng(seed+restart);centers=[X[rng.integers(n)].copy()]
        for _ in range(1,k):
            d=np.min(((X[:,None,:]-np.array(centers)[None,:,:])**2).sum(2),axis=1)
            if d.sum()<1e-12:break
            centers.append(X[rng.choice(n,p=d/d.sum())].copy())
        if len(centers)!=k:continue
        centers=np.array(centers);last=np.full(n,-1)
        for _ in range(200):
            labels=np.argmin(((X[:,None,:]-centers[None,:,:])**2).sum(2),axis=1)
            if len(np.unique(labels))!=k:break
            if np.array_equal(last,labels):break
            last=labels.copy();centers=np.array([X[labels==c].mean(0) for c in range(k)])
        if len(np.unique(labels))!=k:continue
        loss=float(((X-centers[labels])**2).sum())
        if best is None or loss<best[0]:best=(loss,labels.copy(),centers.copy())
    if best is None:raise ValueError('No nonempty K-means solution')
    return best
def silhouette(X,labels):
    distances=np.sqrt(np.maximum(0,((X[:,None,:]-X[None,:,:])**2).sum(2)));out=[]
    for i,c in enumerate(labels):
        own=labels==c;own[i]=False
        if not own.any():out.append(0.);continue
        a=distances[i,own].mean();b=min(distances[i,labels==d].mean() for d in np.unique(labels) if d!=c)
        out.append((b-a)/max(a,b) if max(a,b)>0 else 0.)
    return float(np.mean(out))
def ari(a,b):
    def pairs(x):return x*(x-1)/2
    n=len(a);both=Counter(zip(a,b));sa=sum(pairs(v) for v in Counter(a).values());sb=sum(pairs(v) for v in Counter(b).values());obs=sum(pairs(v) for v in both.values());total=pairs(n)
    if total==0:return 1.
    expected=sa*sb/total;den=(sa+sb)/2-expected
    return float((obs-expected)/den) if den else 1.
def normalized_mi(a,b):
    n=len(a);ca=Counter(a);cb=Counter(b);joint=Counter(zip(a,b))
    h=lambda c:-sum(v/n*np.log(v/n) for v in c.values())
    den=(h(ca)+h(cb))/2
    return float(sum(v/n*np.log(v*n/(ca[x]*cb[y])) for (x,y),v in joint.items())/den) if den else 0.
def robust_scale(A):
    center=np.median(A,axis=0);scale=np.quantile(A,.75,axis=0)-np.quantile(A,.25,axis=0)
    fallback=scale<=1e-12;scale[fallback]=np.std(A,axis=0)[fallback];scale[scale<=1e-12]=1
    return np.clip((A-center)/scale,-5,5),center,scale,fallback
def change_points(X,min_size=4,factor=1.):
    """Exact penalized SSE dynamic program. Analyst-set penalty, NOT a significance test."""
    n,d=X.shape;penalty=float(factor*d*np.log(max(2,n)));S=np.vstack([np.zeros(d),np.cumsum(X,axis=0)]);Q=np.r_[0,np.cumsum((X*X).sum(1))]
    dp=np.full(n+1,np.inf);dp[0]=-penalty;previous=np.full(n+1,-1,int)
    for end in range(min_size,n+1):
        starts=np.arange(end-min_size+1);starts=starts[np.isfinite(dp[starts])]
        if not len(starts):continue
        cost=Q[end]-Q[starts]-((S[end]-S[starts])**2).sum(1)/(end-starts)
        candidate=dp[starts]+np.maximum(0,cost)+penalty;i=int(np.argmin(candidate));dp[end]=candidate[i];previous[end]=starts[i]
    ends=[];cur=n
    if previous[cur]<0:return [],penalty
    while previous[cur]>0:cur=int(previous[cur]);ends.append(cur)
    return sorted(ends),penalty

class StudyData:
    def __init__(self,root):
        self.root=Path(root).resolve();self.sources={};self.cache={}
        self.manifest=self.read_json('export_manifest.json');assert self.manifest['closed'] is True
        self.duration=float(self.manifest['durationSeconds']);self.sid=self.manifest['sessionId'];self.participant=self.manifest.get('participantId','')
        self.regions=self.read_csv('region_intervals.csv');self.ra=number(self.regions.start_session_s);self.rb=number(self.regions.end_session_s)
        good=self.regions.region.eq('Pre-room').to_numpy()&np.isfinite(self.ra)&(self.rb>self.ra);assert good.any(),'No recorded Pre-room boundary'
        self.start=float(self.ra[good].min())
        self.annotations_path=self.root/'Analysis/20261116T151837988Z-3fd29194-annotations-r3.json'
        self.bundle=self.read_json(self.annotations_path);assert self.bundle['session']==self.sid
        self.annotations=self.bundle['annotations'];self.media=self.read_json('media_capture.json')
        self.map=self.read_json('Analysis/target-object-ownership.json')
        assert all(sha(self.root/p)==v for p,v in self.map['provenance']['original_sha256'].items())
        self.Odir=max((self.root/'Analysis').glob('ObjectStatistics-*'),key=lambda p:p.stat().st_mtime)
        self.O=self.read_csv(self.Odir/'gaze_object_hits.csv');self.catalog=self.read_csv('target_catalog.csv')
        self.T=self.read_csv('gaze_targets.csv');self.tt=number(self.T.session_time_s)
        self.E=self.read_csv('game_events.csv');self.et=number(self.E.session_time_s)
        self.raw_hits=yes(self.T.ray_valid)&self.T.status.eq('target_hit').to_numpy()&self.T.target_id.ne('').to_numpy()
        self.ids=np.full(len(self.T),'',dtype=object);self.names={}
        for r in self.O.itertuples(index=False):
            i=int(r.raw_callback_record)-1;assert self.raw_hits[i] and self.T.iloc[i].target_id==r.raw_target_id
            self.ids[i]=r.object_id;self.names[r.object_id]=r.object_name
        assert (self.ids!='').sum()==self.raw_hits.sum()==len(self.O)==16136
        self.ray_valid=yes(self.T.ray_valid)&np.isfinite(self.tt)&self.T.session_id.eq(self.sid).to_numpy()
        self.direction=np.stack([number(self.T[c]) for c in ['ray_direction_x','ray_direction_y','ray_direction_z']],1)
        self.context=self.T[['source','calibration_id','correction_id','mapping_verified']].agg('|'.join,axis=1).to_numpy()
        self.nodes=self.ids.copy();path=self.T.query_collider_path
        self.board_black=self.ray_valid&self.T.status.eq('non_target_occluder').to_numpy()&path.str.endswith('/ChalkBaord Front[0]').to_numpy()
        self.board_white=self.ray_valid&self.T.status.eq('non_target_occluder').to_numpy()&path.str.endswith('/Hub Puzzle_ Cooking Pot Whitboard[0]').to_numpy()
        self.nodes[self.board_black]='surface:unconfigured_blackboard';self.nodes[self.board_white]='surface:unconfigured_whiteboard'
        self.names.update({'surface:unconfigured_blackboard':'Blackboard contact (historically unconfigured)','surface:unconfigured_whiteboard':'Whiteboard contact (historically unconfigured)'})
        self.ray_cover=PairCoverage(self.tt,self.ray_valid,self.context)
        neon=list(self.root.glob('Neon-*/NeonGaze.csv'));assert len(neon)==1,'Explicit multi-stream reconciliation required'
        assert self.read_json(neon[0].parent/'session.json')['closed'] is True
        self.G=self.read_csv(neon[0],usecols=['device_timestamp_ms','connection_epoch','worn','eye_state_available','pupil_left_mm','pupil_right_mm','aligned_session_time_s','time_alignment_status','sample_quality','gaze_x_px','gaze_y_px','clock_half_rtt_plus_resolution_ms'])
        self.gt=number(self.G.aligned_session_time_s);self.pl=number(self.G.pupil_left_mm);self.pr=number(self.G.pupil_right_mm);self.p=(self.pl+self.pr)/2;self.gx=number(self.G.gaze_x_px);self.gy=number(self.G.gaze_y_px)
        aligned=self.G.time_alignment_status.eq('time_echo_estimated').to_numpy()&np.isfinite(self.gt)
        previous_max=np.r_[-np.inf,np.maximum.accumulate(np.where(aligned,self.gt,-np.inf))[:-1]]
        self.monotonic=aligned&(self.gt>previous_max)
        self.native=self.monotonic&yes(self.G.worn)&yes(self.G.eye_state_available)&(number(self.G.device_timestamp_ms)>0)&np.isfinite(self.pl)&np.isfinite(self.pr)&(self.pl>0)&(self.pr>0)&self.G.sample_quality.str.startswith('basic_valid').to_numpy()
        self.epoch=self.G.connection_epoch.to_numpy();dt=np.diff(self.gt);pair=self.native[:-1]&self.native[1:]&(dt>0)&(dt<=.05)&(self.epoch[:-1]==self.epoch[1:])
        v=np.maximum(np.abs(np.diff(self.pl)),np.abs(np.diff(self.pr)))/np.where(dt>0,dt,np.nan)
        candidate=v[pair&(self.gt[1:]>=self.start)&(self.gt[1:]<self.duration)]
        vm=median(candidate);mad=median(np.abs(candidate-vm));self.velocity=v;self.velocity_pair=pair;self.vm=vm;self.vmad=mad;self.speed_threshold=float(vm+6*1.4826*mad)
        rapid=pair&(v>self.speed_threshold);seed=np.zeros(len(self.G),bool);seed[:-1]|=rapid;seed[1:]|=rapid
        bad_times=np.sort(self.gt[seed&np.isfinite(self.gt)]);padded=(np.searchsorted(bad_times,self.gt+.1,'right')-np.searchsorted(bad_times,self.gt-.1,'left'))>0
        self.screen=self.native&~padded
        self.pupil_cover=PairCoverage(self.gt,self.screen,self.epoch);self.native_cover=PairCoverage(self.gt,self.native,self.epoch)
        self.quality=dict(native_rows=int(self.native.sum()),algorithm_screened_rows=int(self.screen.sum()),rapid_pair_seeds=int(rapid.sum()),velocity_threshold_mm_s=self.speed_threshold,mad_multiplier=6,padding_s=.1,interpolation='none',meaning='Heuristic rapid-change screen, NOT validated blink detection or physiological calibration',non_monotonic_or_unaligned=int((~self.monotonic).sum()))
        self.gregion=self.region_array(self.gt);self.region_pupil={r:median(self.p[self.screen&(self.gregion==r)&(self.gt>=self.start)&(self.gt<self.duration)]) for r in self.regions.region.unique()}
        self.region_native={r:median(self.p[self.native&(self.gregion==r)&(self.gt>=self.start)&(self.gt<self.duration)]) for r in self.regions.region.unique()}
        self.H=self.read_csv('head_pose.csv');self.ht=number(self.H.session_time_s);q=np.stack([number(self.H[c]) for c in ['qx','qy','qz','qw']],1);pos=np.stack([number(self.H[c]) for c in ['x','y','z']],1);norm=np.linalg.norm(q,axis=1);nq=q/np.where(norm>0,norm,1)[:,None];hdt=np.diff(self.ht)
        angle=np.degrees(2*np.arccos(np.clip(np.abs((nq[:-1]*nq[1:]).sum(1)),0,1)));dist=np.linalg.norm(np.diff(pos,axis=0),axis=1)
        self.hs=angle/np.where(hdt>0,hdt,np.nan);qgood=np.isfinite(q).all(1)&(norm>.9)&(norm<1.1)
        self.hraw=qgood[:-1]&qgood[1:]&(hdt>0)&(hdt<=.1)
        self.hgood=self.hraw&(dist<=1)&(angle<=30)
        self.head_discontinuities=int((self.hraw&~self.hgood).sum())
        details=[]
        for text in self.E.detail:
            try:details.append(json.loads(text) if text.startswith('{') else {})
            except ValueError:details.append({})
        self.details=details;self.is_target_status=self.E.event_type.eq('target_puzzle_status').to_numpy()
        self.is_insertion=self.is_target_status&np.array([' - Filled ' in str(d.get('legacy_object_id','')) for d in details])
        self.is_wrong=self.E.outcome.eq('WrongMove').to_numpy();self.is_right=self.E.outcome.eq('RightMove').to_numpy();self.is_grab=self.E.event_type.eq('grab_start').to_numpy()
        self.P=self.read_csv('Test-User21_PuzzleLogs.csv');anchors=self.read_csv('clock_anchors.csv');base=median(number(anchors.pc_utc_ms)-(number(anchors.session_time_before_s)+number(anchors.session_time_after_s))*500)
        self.pt=(pd.to_datetime(self.P.TimeStampUTC,utc=True).astype('int64').to_numpy()/1e6-base)/1000
        self.logical=self.P.ElementType.eq('Puzzle').to_numpy()
        self.logical_right=self.logical&self.P.Outcome.eq('RightMove').to_numpy()
        self.quality.update(raw_rows=len(self.G),full_callbacks=len(self.T),mapping_verified_callbacks=int(yes(self.T.mapping_verified).sum()),head_world_discontinuity_pairs_excluded=self.head_discontinuities)
        self.quality['native_study_covered_fraction']=self.native_cover.at(self.start,self.duration)/(self.duration-self.start)
        self.quality['screened_study_covered_fraction']=self.pupil_cover.at(self.start,self.duration)/(self.duration-self.start)

    def checked(self,p):
        p=Path(p);p=p if p.is_absolute() else self.root/p;p=p.resolve();assert p.is_relative_to(self.root)
        self.sources[str(p.relative_to(self.root)).replace('\\','/')]=sha(p);return p
    def read_json(self,p):return js(self.checked(p))
    def read_csv(self,p,**kwargs):return pd.read_csv(self.checked(p),dtype=str,keep_default_na=False,**kwargs)
    def region_array(self,t):
        values=np.full(len(t),'unrecorded_region',dtype=object)
        for i,r in enumerate(self.regions.region):values[(t>=self.ra[i])&(t<self.rb[i])]=r
        return values
    def region(self,a,b):
        seconds=Counter()
        for i,r in enumerate(self.regions.region):seconds[r]+=max(0,min(b,self.rb[i])-max(a,self.ra[i]))
        return seconds.most_common(1)[0][0] if seconds else 'unrecorded_region'
    def region_fraction(self,a,b,r):return sum(max(0,min(b,self.rb[i])-max(a,self.ra[i])) for i,v in enumerate(self.regions.region) if v==r)/(b-a)
    def feature(self,a,b):
        key=(round(a,9),round(b,9))
        if key in self.cache:return self.cache[key].copy()
        assert self.start-1e-6<=a<b<=self.duration+1e-6
        duration=b-a;region=self.region(a,b);g=self.screen&(self.gt>=a)&(self.gt<b);native=self.native&(self.gt>=a)&(self.gt<b)
        tm=(self.tt>=a)&(self.tt<b);idx=np.flatnonzero(tm);hits=0;visits=0;switches=0;counts=Counter();transitions=Counter();prev=None;nodeprev=None
        for i in idx:
            valid=bool(self.ray_valid[i]);hit=bool(self.raw_hits[i]);t=self.tt[i];obj=self.ids[i];context=self.context[i]
            continuous=prev is not None and i==prev[0]+1 and 0<t-prev[1]<=.1 and context==prev[2]
            if hit:
                hits+=1;counts[obj]+=1
                if not(continuous and prev[3] and prev[4]==obj):visits+=1
                if continuous and prev[3] and prev[4]!=obj:switches+=1
            node=self.nodes[i]
            if node and continuous and nodeprev and node!=nodeprev:transitions[(nodeprev,node)]+=1
            nodeprev=node if valid else None;prev=(i,t,context,hit,obj)
        em=(self.et>=a)&(self.et<b);hm=(self.ht[:-1]>=a)&(self.ht[1:]<b)
        pmed=median(self.p[g]);hs=median(self.hs[hm&self.hgood]);total=counts.total();prob=np.array(list(counts.values()),float)/total if total else np.array([])
        rays=self.direction[tm&self.ray_valid];rays=rays[np.isfinite(rays).all(1)];norm=np.linalg.norm(rays,axis=1);rays=rays[(norm>.5)&(norm<1.5)];angles=np.array([])
        if len(rays):
            rays=rays/np.linalg.norm(rays,axis=1)[:,None];center=rays.mean(0);cnorm=np.linalg.norm(center)
            if cnorm>1e-6:angles=np.degrees(np.arccos(np.clip(rays@(center/cnorm),-1,1)))
        result=dict(start_s=a,end_s=b,duration_s=duration,region=region,region_fraction=self.region_fraction(a,b,region),native_eye_rows=int(native.sum()),screened_eye_rows=int(g.sum()),native_pupil_coverage=self.native_cover.at(a,b)/duration,screened_pupil_coverage=self.pupil_cover.at(a,b)/duration,
                    pupil_raw_median_mm=median(self.p[native]),pupil_raw_iqr_mm=iqr(self.p[native]),pupil_raw_region_centered_mm=median(self.p[native])-self.region_native.get(region,np.nan),pupil_screened_median_mm=pmed,pupil_left_screened_median_mm=median(self.pl[g]),pupil_right_screened_median_mm=median(self.pr[g]),pupil_lr_abs_difference_median_mm=median(np.abs(self.pl[g]-self.pr[g])),pupil_iqr_mm=iqr(self.p[g]),pupil_region_centered_mm=pmed-self.region_pupil.get(region,np.nan),
                    head_camera_median_deg_s=hs,head_camera_raw_median_deg_s=median(self.hs[hm&self.hraw]),head_log1p_median_deg_s=float(np.log1p(hs)) if np.isfinite(hs) else np.nan,
                    ray_callbacks=int(tm.sum()),valid_ray_callbacks=int((tm&self.ray_valid).sum()),valid_ray_coverage=self.ray_cover.at(a,b)/duration,target_hit_callbacks=hits,target_contact_fraction=hits/int(tm.sum()) if tm.any() else np.nan,unique_hit_objects=len(counts),target_visit_segments=visits,target_visits_per_s=visits/duration if (tm&self.ray_valid).any() else np.nan,target_switches=switches,target_switches_per_s=switches/duration if (tm&self.ray_valid).any() else np.nan,target_entropy_bits=float(-(prob*np.log2(prob)).sum()) if len(prob) else np.nan,
                    board_contact_callbacks=int((tm&(self.board_black|self.board_white)).sum()),board_contact_fraction=float((tm&(self.board_black|self.board_white)).sum())/int(tm.sum()) if tm.any() else np.nan,grab_starts=int((em&self.is_grab).sum()),grab_starts_per_s=float((em&self.is_grab).sum())/duration,
                    wrong_insertion_verdicts=int((em&self.is_insertion&self.is_wrong).sum()),right_insertion_verdicts=int((em&self.is_insertion&self.is_right).sum()),wrong_insertion_verdicts_per_s=float((em&self.is_insertion&self.is_wrong).sum())/duration,right_insertion_verdicts_per_s=float((em&self.is_insertion&self.is_right).sum())/duration,
                    logical_right_status_records=int(((self.pt>=a)&(self.pt<b)&self.logical_right).sum()),annotation_overlap_fraction=sum(max(0,min(b,float(x['end_s']))-max(a,float(x['start_s']))) for x in self.annotations)/duration,
                    raw_scene_x_iqr_px=iqr(self.gx[native]),raw_scene_y_iqr_px=iqr(self.gy[native]),world_ray_direction_median_spread_deg=median(angles),
                    top_objects=[dict(id=x,name=self.names.get(x,x),callbacks=n) for x,n in counts.most_common(5)],transitions=[dict(source=x,target=y,count=n) for (x,y),n in transitions.most_common()])
        self.cache[key]=result;return result.copy()

def make_windows(data,size):
    windows=[]
    for i,a in enumerate(np.arange(data.start,data.duration,size)):
        b=min(data.duration,a+size);f=data.feature(float(a),float(b));f['window_id']=f'W{size:g}-{i+1:04d}'
        f['full_window']=b-a>=size-1e-6
        f['model_eligible']=bool(f['full_window'] and f['screened_pupil_coverage']>=.6 and f['valid_ray_coverage']>=.6 and f['region_fraction']>=.8 and all(np.isfinite(f[k]) for k in FEATURES))
        f['algorithm_pattern_id']='';f['analysis_scope']='Study_original_session_seconds'
        f['annotation_overlaps']=[dict(id=x['id'],label=x['label'],review_status=x.get('review_status','draft'),fraction=max(0,min(b,float(x['end_s']))-max(a,float(x['start_s'])))/(b-a)) for x in data.annotations if overlaps(a,b,float(x['start_s']),float(x['end_s']))]
        windows.append(f)
    return windows

def model_windows(windows,features=FEATURES,k_fixed=None):
    selected=[i for i,w in enumerate(windows) if w['model_eligible']];assert len(selected)>=12
    A=np.array([[windows[i][f] for f in features] for i in selected]);keep=np.ptp(A,axis=0)>1e-12;features=[f for f,k in zip(features,keep) if k];A=A[:,keep]
    X,center,scale,fallback=robust_scale(A);candidates=[];fits={}
    for k in ([k_fixed] if k_fixed else range(2,6)):
        loss,labels,centers=kmeans(X,k);sizes=np.bincount(labels,minlength=k);score=silhouette(X,labels);acceptable=bool(sizes.min()>=max(5,math.ceil(.05*len(X))))
        candidates.append(dict(k=k,silhouette=score,minimum_cluster_windows=int(sizes.min()),minimum_size_rule_pass=acceptable,sse=loss));fits[k]=(labels,centers)
    choices=[r for r in candidates if r['minimum_size_rule_pass']]
    if not choices:choices=candidates
    winner=max(choices,key=lambda r:r['silhouette']);k=winner['k'];labels,centers=fits[k]
    order=sorted(range(k),key=lambda c:float(np.median([windows[selected[i]]['head_camera_median_deg_s'] for i in np.flatnonzero(labels==c)])))
    rename={c:j for j,c in enumerate(order)};labels=np.array([rename[c] for c in labels]);centers=np.array([centers[c] for c in order])
    patterns=[]
    for c in range(k):
        ids=np.flatnonzero(labels==c);rows=[windows[selected[i]] for i in ids];region_counts=Counter(r['region'] for r in rows)
        profile={f:median([r[f] for r in rows]) for f in features}
        patterns.append(dict(pattern_id=f'P{c+1:02d}',windows=len(rows),seconds=sum(r['duration_s'] for r in rows),region_counts=dict(region_counts),dominant_region=region_counts.most_common(1)[0][0],dominant_region_fraction=region_counts.most_common(1)[0][1]/len(rows),median_features=profile,median_pupil_mm=median([r['pupil_screened_median_mm'] for r in rows]),median_head_deg_s=median([r['head_camera_median_deg_s'] for r in rows])))
    for j,i in enumerate(selected):windows[i]['algorithm_pattern_id']=f'P{labels[j]+1:02d}'
    U,S,Vt=np.linalg.svd(X-X.mean(0),full_matrices=False);coords=(U*S)[:,:2];var=S*S/(len(X)-1);var=var/var.sum()
    stability=[]
    for seed in (17,43,91,211):stability.append(ari(labels,kmeans(X,k,seed=seed,restarts=6)[1]))
    summary=dict(method='Deterministic K-means++ / Lloyd, exploratory only',selected_k=k,candidates=candidates,features=features,eligible_windows=len(X),total_windows=len(windows),median_center=center.tolist(),scale=scale.tolist(),std_fallback_features=[f for f,v in zip(features,fallback) if v],normalized_feature_clip=[-5,5],patterns=patterns,seed_stability_ARI=stability,pattern_region_NMI=normalized_mi(labels,[windows[i]['region'] for i in selected]),pca_explained_fraction=var[:2].tolist(),selection_rule='Highest descriptive silhouette among k=2..5 with >=5 windows and >=5% in every group; fallback disclosed if no candidate meets minimum size',minimum_size_candidate_available=bool(any(r['minimum_size_rule_pass'] for r in candidates)),meaning='Unsupervised recorded-feature patterns, NOT cognitive-state labels or predictive validation')
    return summary,dict(X=X,A=A,indices=selected,labels=labels,centers=centers,coordinates=coords)

def episode_comparisons(data):
    episodes=[];control_rows=[];filter_rows=[]
    for annotation in data.annotations:
        a=float(annotation['start_s']);b=float(annotation['end_s']);duration=b-a;f=data.feature(a,b);reference=data.feature(max(data.start,a-20),a)
        controls=[];r=f['region']
        for i,region in enumerate(data.regions.region):
            if region!=r:continue
            left=max(data.start,data.ra[i]);right=min(data.duration,data.rb[i]);cursor=left
            while cursor+duration<=right+1e-9:
                end=cursor+duration
                if not any(overlaps(cursor,end,float(x['start_s'])-10,float(x['end_s'])+10) for x in data.annotations):
                    c=data.feature(float(cursor),float(end))
                    if c['screened_pupil_coverage']>=.6 and c['valid_ray_coverage']>=.6 and c['region_fraction']>.99:controls.append(c)
                cursor+=duration
        controls=sorted(controls,key=lambda c:abs((c['start_s']+c['end_s'])/2-(a+b)/2))[:5]
        contrasts={}
        for metric in ['pupil_screened_median_mm','pupil_iqr_mm','target_switches_per_s','target_visits_per_s','target_contact_fraction','head_camera_median_deg_s','grab_starts_per_s']:
            values=np.array([c[metric] for c in controls]);values=values[np.isfinite(values)];v=f[metric]
            contrasts[metric]=dict(episode=v,reference_median=median(values),difference=v-median(values),empirical_percentile=100*((values<v).sum()+.5*(values==v).sum())/len(values) if len(values) and np.isfinite(v) else np.nan)
        for i,c in enumerate(controls):control_rows.append(dict(annotation_id=annotation['id'],annotation_label=annotation['label'],reference_id=f'R{i+1}',reference_status='unlabelled_same_region_NOT_confirmed_nonconfused',**c))
        for multiplier in [0,4,6,8]:
            if multiplier==0:mask=data.native;threshold=None
            else:
                threshold=data.vm+multiplier*1.4826*data.vmad;bad=data.velocity_pair&(data.velocity>threshold);seed=np.r_[bad,False]|np.r_[False,bad];times=np.sort(data.gt[seed&np.isfinite(data.gt)])
                padded=(np.searchsorted(times,data.gt+.1,'right')-np.searchsorted(times,data.gt-.1,'left'))>0;mask=data.native&~padded
            cover=PairCoverage(data.gt,mask,data.epoch);m=median(data.p[mask&(data.gt>=a)&(data.gt<b)]);base=median(data.p[mask&(data.gt>=a-20)&(data.gt<a)])
            filter_rows.append(dict(annotation_id=annotation['id'],label=annotation['label'],mad_multiplier=multiplier,screen='availability_only' if not multiplier else 'rapid_change_heuristic',threshold_mm_s=threshold,episode_pupil_median_mm=m,previous20s_median_mm=base,change_mm=m-base,episode_covered_fraction=cover.at(a,b)/(b-a)))
        episodes.append(dict(annotation=annotation,features=f,previous20s=reference,pupil_change_from_previous20s_mm=f['pupil_screened_median_mm']-reference['pupil_screened_median_mm'],matched_reference_count=len(controls),reference_status='unlabelled_same_region_NOT_confirmed_nonconfused',reference_ids=[dict(start_s=c['start_s'],end_s=c['end_s']) for c in controls],contrasts=contrasts,comparison_limitation='Different tasks and unequal duration; prior20s not neutral. References matched on region and duration only, not luminance or confirmed cognitive state. No p-value or population claim.'))
    return episodes,control_rows,filter_rows

def event_locked(data):
    indices=np.flatnonzero(data.is_insertion&(data.et>=data.start)&(data.et<data.duration));times=data.et[indices];records=[];curves=[]
    for i,t in zip(indices,times):
        region=data.region(t-.1,t+.1);a=t-2;b=t+4;reason=[]
        if a<data.start or b>data.duration:reason.append('outside_study_or_session')
        if data.region_fraction(max(data.start,a),min(data.duration,b),region)<.99:reason.append('crosses_region_boundary')
        base_coverage=data.pupil_cover.at(a,t-.5)/1.5;post_coverage=data.pupil_cover.at(t+.5,t+3)/2.5
        if base_coverage<.6 or post_coverage<.6:reason.append('insufficient_screened_pupil_coverage')
        baseline=median(data.p[data.screen&(data.gt>=a)&(data.gt<t-.5)]);post=median(data.p[data.screen&(data.gt>=t+.5)&(data.gt<t+3)])
        neighbors=int(((times>=a)&(times<b)&(np.abs(times-t)>1e-9)).sum());isolated=neighbors==0
        row=dict(event_id=data.E.iloc[i].event_id,session_time_s=float(t),region=region,outcome=data.E.iloc[i].outcome,target_id=data.E.iloc[i].object_id,legacy_id=data.details[i].get('legacy_object_id',''),baseline_median_mm=baseline,post_median_mm=post,post_change_mm=post-baseline,baseline_coverage=base_coverage,post_coverage=post_coverage,other_insertion_records_in_minus2_plus4s=neighbors,isolated=isolated,quality_eligible=not reason,exclusion_reasons=';'.join(reason))
        records.append(row)
        for rel in np.arange(-2,4,.5):
            lo=t+rel;hi=lo+.5;coverage=data.pupil_cover.at(lo,hi)/.5
            delta=median(data.p[data.screen&(data.gt>=lo)&(data.gt<hi)])-baseline if coverage>=.6 and not reason else np.nan
            curves.append(dict(event_id=row['event_id'],outcome=row['outcome'],region=region,isolated=isolated,quality_eligible=not reason,relative_start_s=float(rel),relative_end_s=float(rel+.5),pupil_change_mm=delta,bin_coverage=coverage))
    summary=[]
    for isolation in [False,True]:
        for outcome in ['WrongMove','RightMove']:
            rows=[r for r in records if r['outcome']==outcome and r['quality_eligible'] and (r['isolated'] or not isolation)]
            summary.append(dict(event_set='isolated_only' if isolation else 'all_quality_eligible_overlap_retained',outcome=outcome,n_events=len(rows),median_post_change_mm=median([r['post_change_mm'] for r in rows]),meaning='Within-event association, not outcome-caused physiology; event neighborhoods may overlap in the all-events set'))
    averaged=[]
    for isolation in [False,True]:
        for outcome in ['WrongMove','RightMove']:
            for rel in np.arange(-2,4,.5):
                vals=[r['pupil_change_mm'] for r in curves if r['outcome']==outcome and r['quality_eligible'] and r['relative_start_s']==rel and (r['isolated'] or not isolation) and np.isfinite(r['pupil_change_mm'])]
                averaged.append(dict(event_set='isolated_only' if isolation else 'all_quality_eligible_overlap_retained',outcome=outcome,relative_mid_s=float(rel+.25),n_events=len(vals),median_change_mm=median(vals),q25_change_mm=float(np.quantile(vals,.25)) if vals else np.nan,q75_change_mm=float(np.quantile(vals,.75)) if vals else np.nan))
    return records,curves,summary,averaged

def detect_changes(windows,model):
    indices=model['indices'];runs=[];run=[]
    for j,i in enumerate(indices):
        if run:
            previous=windows[indices[run[-1]]]
            if abs(previous['end_s']-windows[i]['start_s'])>1e-5 or previous['region']!=windows[i]['region']:runs.append(run);run=[]
        run.append(j)
    if run:runs.append(run)
    results=[]
    for run in runs:
        if len(run)<8:continue
        X=model['X'][run];by_factor={}
        for factor in [.5,1.,2.]:by_factor[factor]=change_points(X,4,factor)[0]
        for boundary in by_factor[1.]:
            j=run[boundary];i=indices[j];time=windows[i]['start_s'];support=sum(any(abs(b-boundary)<=2 for b in vals) for vals in by_factor.values())
            results.append(dict(session_time_s=time,region=windows[i]['region'],penalty_support_of3=support,minimum_segment_s=20,method='within-region contiguous-quality-run penalized multivariate mean shift',meaning='Feature-distribution change, NOT cognitive-state onset',neighboring_window_ids=[windows[indices[run[boundary-1]]]['window_id'],windows[i]['window_id']]))
    return results

def run_analysis(data):
    print('Building five-second features and exact annotated-episode comparisons...',flush=True)
    windows=make_windows(data,5.);episodes,references,filter_sensitivity=episode_comparisons(data)
    print('Fitting unlabeled patterns, sensitivity checks and within-region change points...',flush=True)
    model_summary,model=model_windows(windows)
    stability=[]
    for size in [10.,20.]:
        other=make_windows(data,size);summary,_=model_windows(other);stability.append(dict(window_s=size,selected_k=summary['selected_k'],silhouette=next(r['silhouette'] for r in summary['candidates'] if r['k']==summary['selected_k']),eligible_windows=summary['eligible_windows'],pattern_region_NMI=summary['pattern_region_NMI']))
    raw_windows=[dict(w,pupil_region_centered_mm=w['pupil_raw_region_centered_mm'],pupil_iqr_mm=w['pupil_raw_iqr_mm']) for w in windows];raw_summary,raw_model=model_windows(raw_windows,k_fixed=model_summary['selected_k']);raw_ari=ari(model['labels'],raw_model['labels'])
    no_pupil=[f for f in FEATURES if not f.startswith('pupil')];np_summary,np_model=model_windows([w.copy() for w in windows],features=no_pupil,k_fixed=model_summary['selected_k']);no_pupil_ari=ari(model['labels'],np_model['labels'])
    hub=[w.copy() for w in windows if w['region']=='Hub'];hub_summary,hub_model=model_windows(hub)
    changes=detect_changes(windows,model);no_pupil_changes=detect_changes(windows,np_model);events,curves,event_summary,event_average=event_locked(data)
    for c in changes:c['also_detected_without_pupil_features']=any(abs(c['session_time_s']-n['session_time_s'])<=10 for n in no_pupil_changes)
    exact=data.feature(data.start,data.duration);assert exact['target_hit_callbacks']==12726 and exact['grab_starts']==119
    assert sum(w['target_hit_callbacks'] for w in windows)==12726
    pattern_episodes=[]
    for e in episodes:
        a=float(e['annotation']['start_s']);b=float(e['annotation']['end_s']);seconds=Counter();eligible=0.
        for w in windows:
            overlap=max(0,min(b,w['end_s'])-max(a,w['start_s']))
            if overlap and w['algorithm_pattern_id']:seconds[w['algorithm_pattern_id']]+=overlap;eligible+=overlap
        pattern_episodes.append(dict(label=e['annotation']['label'],annotation_id=e['annotation']['id'],pattern_seconds=dict(seconds),modeled_seconds=eligible,meaning='Post-hoc overlap, not trained/predicted labels; short episodes can span partially labeled windows'))
    sensitivity=dict(window_sizes=stability,raw_vs_screened_pupil_fixed_k_ARI=raw_ari,without_pupil_features_fixed_k_ARI=no_pupil_ari,change_points_without_pupil=no_pupil_changes,interpretation='Algorithm dependence only, NOT accuracy, causal importance or cognitive-state separability')
    print(json.dumps(clean_json(dict(quality=data.quality,model={k:model_summary[k] for k in ['selected_k','eligible_windows','pattern_region_NMI','patterns']},hub_model={k:hub_summary[k] for k in ['selected_k','eligible_windows','pattern_region_NMI','patterns']},changes=changes,event_summary=event_summary,sensitivity=sensitivity,episode_references=[dict(label=e['annotation']['label'],n=e['matched_reference_count'],pupil=e['contrasts']['pupil_screened_median_mm']) for e in episodes])),indent=2),flush=True)
    return dict(data=data,windows=windows,episodes=episodes,references=references,filter_sensitivity=filter_sensitivity,model_summary=model_summary,model=model,hub_windows=hub,hub_summary=hub_summary,hub_model=hub_model,changes=changes,event_records=events,event_curves=curves,event_summary=event_summary,event_average=event_average,sensitivity=sensitivity,pattern_episodes=pattern_episodes,exact=exact)

def publish(result,output=None):
    data=result['data'];stamp=datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ');out=(Path(output) if output else data.root/'Analysis'/('ExploratoryMultimodal-'+stamp)).resolve()
    assert out.parent==data.root/'Analysis','Output must be a NEW immediate child of this session Analysis directory'
    if out.exists():raise ValueError('Output exists; refusing overwrite')
    assert all(sha(data.root/name)==value for name,value in data.sources.items()),'An input changed during analysis'
    out.mkdir(parents=False)
    numeric_windows=[{k:v for k,v in w.items() if k not in ['transitions','top_objects']} for w in result['windows']]
    write_csv(out/'window_features_5s.csv',numeric_windows)
    write_csv(out/'episode_features.csv',[dict(annotation_id=e['annotation']['id'],label=e['annotation']['label'],review_status=e['annotation'].get('review_status','draft'),participant_intensity=e['annotation'].get('participant_intensity'),rating_scale=e['annotation'].get('intensity_scale'),matched_unlabelled_reference_count=e['matched_reference_count'],pupil_change_from_previous20s_mm=e['pupil_change_from_previous20s_mm'],**{k:v for k,v in e['features'].items() if k not in ['transitions','top_objects']}) for e in result['episodes']])
    write_csv(out/'matched_unlabelled_references.csv',[{k:v for k,v in c.items() if k not in ['transitions','top_objects']} for c in result['references']])
    write_csv(out/'pupil_filter_sensitivity.csv',result['filter_sensitivity'])
    write_csv(out/'insertion_event_features.csv',result['event_records'])
    write_csv(out/'insertion_pupil_curves.csv',result['event_curves'])
    write_csv(out/'insertion_event_summary.csv',result['event_summary'])
    write_csv(out/'change_points.csv',result['changes'],['session_time_s','region','penalty_support_of3','minimum_segment_s','method','meaning','neighboring_window_ids','also_detected_without_pupil_features'])
    write_csv(out/'global_pattern_profiles.csv',[dict(pattern_id=p['pattern_id'],windows=p['windows'],seconds=p['seconds'],dominant_region=p['dominant_region'],dominant_region_fraction=p['dominant_region_fraction'],**p['median_features']) for p in result['model_summary']['patterns']])
    write_csv(out/'hub_pattern_windows.csv',[{k:v for k,v in w.items() if k not in ['transitions','top_objects']} for w in result['hub_windows']])
    transitions=[dict(source_id=r['source'],source_name=data.names.get(r['source'],r['source']),target_id=r['target'],target_name=data.names.get(r['target'],r['target']),adjacent_callback_transitions=r['count'],definition='consecutive original valid callback pair; <=0.1s; same source/calibration/correction/verification; descriptive NOT saccades') for r in result['exact']['transitions']]
    write_csv(out/'observed_target_surface_transitions.csv',transitions)
    reasons=np.where(~data.native,'unavailable_unaligned_nonmonotonic_or_missing_fields',np.where(data.screen,'','rapid_change_heuristic_padding'))
    pd.DataFrame(dict(source_csv_record=np.arange(1,len(data.G)+1),aligned_session_time_s=data.gt,connection_epoch=data.epoch,pupil_left_recorded_text=data.G.pupil_left_mm,pupil_right_recorded_text=data.G.pupil_right_mm,availability_time_eligible=data.native,heuristic_screen_eligible=data.screen,screen_exclusion_reason=reasons)).to_csv(out/'pupil_sample_screen.csv',index=False,na_rep='')
    summary={k:result[k] for k in ['model_summary','hub_summary','changes','event_summary','event_average','sensitivity','pattern_episodes','filter_sensitivity']}
    summary.update(schema='gazeflow-p027-exploration-v1',participant=data.participant,session=data.sid,analysis_date='2026-11-16',analysis_scope=dict(start_s=data.start,end_s=data.duration,tutorial_excluded=True,clock='original_session_seconds'),quality=data.quality,episodes=result['episodes'],study_totals={k:v for k,v in result['exact'].items() if k not in ['transitions','top_objects']},window_features=numeric_windows,hub_windows=[{k:v for k,v in w.items() if k not in ['transitions','top_objects']} for w in result['hub_windows']],pca=[dict(window_id=result['windows'][i]['window_id'],start_s=result['windows'][i]['start_s'],region=result['windows'][i]['region'],pattern_id=result['windows'][i]['algorithm_pattern_id'],pc1=result['model']['coordinates'][j,0],pc2=result['model']['coordinates'][j,1]) for j,i in enumerate(result['model']['indices'])],region_intervals=[dict(start_s=max(data.start,float(r.start_session_s)),end_s=float(r.end_session_s),region=r.region) for r in data.regions.itertuples() if float(r.end_session_s)>data.start],transitions=transitions[:25],video_offset_s=data.bundle.get('video',{}).get('offset_s',2.117529296875),video_timing_method=data.bundle.get('video',{}).get('timing_method','capture_launch_estimate'),source_inventory=[dict(path=name,sha256=value) for name,value in data.sources.items()])
    late=[c for c in result['changes'] if c['region']=='Hub' and c['session_time_s']>1504]
    summary['hub_change_comparison']=[]
    for c in late:
        t=c['session_time_s'];left=data.feature(float(data.annotations[2]['start_s']),t);right=data.feature(t,data.duration)
        summary['hub_change_comparison'].append(dict(change=c,before={k:v for k,v in left.items() if k not in ['transitions','top_objects']},after={k:v for k,v in right.items() if k not in ['transitions','top_objects']},warning='Offline segmentation uses all time points, including insertion verdicts inside the selected 5-second window. NOT a prospective prediction or a millisecond-accurate psychological onset.'))
    dump(out/'analysis_results.json',summary)
    template=Path(__file__).with_name('p027_exploratory_report.html').read_text(encoding='utf-8')
    payload=json.dumps(clean_json(summary),allow_nan=False,separators=(',',':')).replace('<','\\u003c').replace('>','\\u003e').replace('&','\\u0026')
    assert template.count('__ANALYSIS_DATA__')==1
    (out/'analysis-report.html').write_text(template.replace('__ANALYSIS_DATA__',payload),encoding='utf-8')
    shutil.copyfile(__file__,out/'reproduce_analysis.py');shutil.copyfile(Path(__file__).with_name('p027_exploratory_report.html'),out/'p027_exploratory_report.html')
    c=summary['hub_change_comparison'][0] if summary['hub_change_comparison'] else None
    change_text=(f"At {clock(c['change']['session_time_s'])}, within-Hub mean-shift segmentation found a feature change. Before/after within the final annotation: {c['before']['wrong_insertion_verdicts']}/{c['before']['right_insertion_verdicts']} wrong/right insertion-status records versus {c['after']['wrong_insertion_verdicts']}/{c['after']['right_insertion_verdicts']}. The boundary is a retrospective 5-second-bin boundary using behavioral outcomes too, not a prediction of insight.") if c else 'No primary within-Hub change point met the selected penalty/minimum-duration rule.'
    readme=f'''# P027 exploratory multimodal analysis

Local analysis dated 2026-11-16 (America/New_York). One participant, one session. No recording, annotation, Unity source, main branch or earlier export was changed. No data was uploaded. Open **analysis-report.html**.

## Findings

- K-means selected {result['model_summary']['selected_k']} neutral recorded-feature patterns from {result['model_summary']['eligible_windows']} eligible nonoverlapping five-second windows. Ten- and twenty-second sensitivity runs also selected two groups. These are not validated cognitive states.
- {change_text}
- Confusion has {result['episodes'][0]['matched_reference_count']} equal-duration same-region unlabelled references; Stuck has {result['episodes'][1]['matched_reference_count']}; the long Hub annotation has none. Unlabelled references are NOT confirmed calm/non-confused controls.
- In the exact Stuck interval, screened pupil median was {result['episodes'][1]['features']['pupil_screened_median_mm']:.6f} mm; median across its unlabelled references was {result['episodes'][1]['contrasts']['pupil_screened_median_mm']['reference_median']:.6f} mm. This does not support a simple large-pupil Stuck detector in this session.
- The all-event WrongMove/RightMove pupil comparison is confounded by temporal clustering. After excluding every event with another insertion in [-2,+4) s, zero eligible isolated RightMove events remain. No independent correct-versus-wrong physiological effect is established.

## Data and units

Study is [{data.start:.7f}, {data.duration:.7f}) original session seconds; Pre-room is included, earlier Tutorial excluded but preserved in original exports. Full native Neon pupil/scene-image XY, official mapped ray callbacks, corrected canonical object hits, head/camera poses, game events, logical puzzle records, clock anchors, region intervals and saved human annotation records were inspected/used according to their roles. Pupil sample rows are not independent participants/trials.

Scene-image XY statistics remain pixel diagnostics, not VR target positions. World-ray angular spread includes head/view motion and is not a fixation/saccade measure. Original raw payloads, duplicate legacy event streams and legacy NO_EYE_TRACKER placeholders are not independent analytical observations. Audio/video remain review material; no speech transcription or audio-content inference was performed.

All 12,726 Study hit rows reconcile to the corrected object export. Parent/child IDs are retained. Blackboard/whiteboard contacts remain analytically distinct historically-unconfigured surfaces, not retroactively configured AOI hits. Spatial mapping verification callbacks: 0. Applied direction adjustment is not a residual-accuracy measurement.

## Reproducible methods

1. Require closed manifests and matching session IDs. Source hashes are recorded and rechecked. Never write an input. Preserve distinct aligned-eye, Unity callback and media-launch clocks; no hardware synchronization or latency claim.
2. Native pupil eligibility: estimated aligned monotonic session time, positive finite bilateral values, worn/eye-state/basic decoder flags and positive device time. No zero filling or interpolation. This is availability, not validated physiological quality.
3. Heuristic rapid-change screen: maximum bilateral absolute sample-to-sample derivative, within 0.05 s and same epoch; threshold median + 6 x 1.4826 MAD ({data.speed_threshold:.6f} mm/s); exclude both endpoints and +/-0.1 s. This is NOT a validated blink detector. MAD multipliers 4, 6, 8 and availability-only comparisons are exported. Native Study coverage {data.quality['native_study_covered_fraction']:.2%}; screened {data.quality['screened_study_covered_fraction']:.2%}. Screen exclusion does not prove an original sample was wrong.
4. Head/camera world-quaternion pair speed is descriptive. Exclude pair gaps >0.1 s, position jumps >1 m or rotations >30 degrees per pair; these heuristics do not fully isolate physical head motion from locomotion/recentering. Original pose file remains unchanged.
5. Five-second half-open windows on the Study clock, no overlap. Model only full windows with >=60% screened-pupil coverage, >=60% valid-ray pair coverage, >=80% one recorded region, all model features finite. Partial/missing windows remain in CSV without imputation. Each annotation retains its original interval and draft status. Annotation overlap is post-hoc context, never a training target.
6. Features: {', '.join(FEATURES)}. Regional pupil centering uses the observed region median, NOT a neutral baseline. Median/IQR scaling; zero-IQR columns fall back to standard deviation, constants excluded; normalized model values clipped to [-5,+5]. Sparse event zeros mean zero such logged events, not absence of thought/activity.
7. NumPy K-means++ / Lloyd implementation, seed {SEED}, 16 restarts, k=2..5; select descriptive silhouette subject to every group having >=5 windows and >=5% membership. PCA is SVD for visualization. Four seed re-fits, 10/20 s windows, raw-versus-screened pupil and no-pupil ablations are recorded. No supervised classifier, accuracy claim, significance test or cross-participant generalization.
8. Change-point detection: exact penalized multivariate within-region mean-shift dynamic program, minimum four eligible bins (20 s); do not bridge quality gaps/region changes; penalty D*log(N), sensitivity 0.5/1/2 multipliers. Original Confusion interval is shorter than minimum segment size. Detection failure would not mean absence of confusion. Changes are offline patterns, not cognitive onset, causal effects or prospective predictions.
9. Count only normalized target_puzzle_status rows whose original legacy ID contains ' - Filled ' as insertion-verdict records. Do not double-count raw/normalized puzzle events or parent completion cascades. RightMove is a task verdict, not independent evidence of understanding.
10. Event-locked pupil: [-2,+4) s around insertion status, baseline [-2,-0.5), post [0.5,3), >=60% screened coverage, same recorded region. Curves use 0.5-s bins and equal event weighting. Isolated-only set additionally rejects any other insertion in that window. IQR is not a confidence interval; events share one participant and many all-event neighborhoods overlap.
11. Same-region comparison references have equal episode duration, do not overlap any annotation or its 10-s guard, are nonoverlapping, and pass pupil/ray coverage. Select up to five temporally nearest. No matching on pupil/outcome, no invented calm labels, no invalid significance testing of sensor rows.

## Outputs

- window_features_5s.csv: multimodal features, availability, exact annotation overlap and neutral pattern IDs.
- episode_features.csv / matched_unlabelled_references.csv: exact annotated intervals and explicitly unlabelled comparisons.
- pupil_sample_screen.csv / pupil_filter_sensitivity.csv: every raw pupil source-record reference and the inspectable screening decisions/sensitivity.
- change_points.csv / global_pattern_profiles.csv / hub_pattern_windows.csv: descriptive algorithm outputs.
- insertion_event_features.csv / insertion_pupil_curves.csv / insertion_event_summary.csv: event-level and time-course analyses, including exclusions/overlap.
- observed_target_surface_transitions.csv: consecutive-callback transitions, not saccades. No bridging invalid/no-hit gaps or different correction contexts.
- analysis_results.json / provenance.json: complete numeric report and input/output hashes.
- reproduce_analysis.py and its HTML template: rerun with Python + NumPy + Pandas, e.g. `python reproduce_analysis.py --session "<this original session>"`. A new unique Analysis subfolder is created; existing outputs are never overwritten.

## Interpretation and next evidence

Use the change point and contrasting recorded-feature patterns as replay/interview prompts. Confirm whether the player experienced a strategy change; preserve the broad original label and revision history. Add reviewed smooth-progress comparison episodes before supervised learning. Confusion and inability to proceed may co-occur and need not be exclusive labels. Neither rays nor pupil size alone establish attention, comprehension, confusion or insight. No retinal luminance measurement is present.

Method references: [Mathot et al., baseline correction](https://pmc.ncbi.nlm.nih.gov/articles/PMC5809553/); [Pan et al., luminance and pupil arousal](https://www.nature.com/articles/s41598-022-05280-1); [K-means overview](https://scikit-learn.org/stable/modules/clustering.html#k-means). The implementation here uses NumPy, not a bundled scikit-learn installation.
'''
    (out/'README.md').write_text(readme,encoding='utf-8')
    assert all(sha(data.root/name)==value for name,value in data.sources.items()),'Source changed during output publication'
    outputs={p.name:dict(bytes=p.stat().st_size,sha256=sha(p)) for p in out.iterdir() if p.is_file()}
    dump(out/'provenance.json',dict(schema='gazeflow-exploratory-provenance-v1',created_utc=datetime.now(timezone.utc).isoformat(),session=data.sid,analysis_scope=summary['analysis_scope'],input_sha256=data.sources,output_files=outputs,software=dict(python=sys.version,numpy=np.__version__,pandas=pd.__version__),random_seed=SEED,source_immutability_verified=True,complete=True))
    pointer=Path(__file__).with_name('p027-exploratory-latest.json');dump(pointer,dict(folder=str(out),html=str(out/'analysis-report.html'),results=str(out/'analysis_results.json')))
    print('ANALYSIS_REPORT:',out/'analysis-report.html',flush=True)
    return out

def self_test():
    X=np.array([[0.,0.],[0.,.1],[.1,0.],[8.,8.],[8.,8.1],[8.1,8.]])
    _,labels,_=kmeans(X,2);assert len(set(labels[:3]))==len(set(labels[3:]))==1 and labels[0]!=labels[-1]
    assert silhouette(X,labels)>.95 and ari(labels,1-labels)==1 and normalized_mi(labels,1-labels)> .99
    pts,_=change_points(np.r_[np.zeros((10,2)),np.ones((10,2))*10],min_size=4);assert pts==[10]
    cover=PairCoverage(np.array([0.,.05,.1,.15,1.]),np.array([True,True,False,True,True]));assert abs(cover.at(0,.2)-.05)<1e-12 and cover.at(.01,.2)==0
    print('PASS: synthetic K-means, silhouette, label-permutation ARI, change point and gap-safe coverage.',flush=True)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--session',type=Path,required=True,help='Private original P027 session folder; raw inputs are not distributed');parser.add_argument('--inspect',action='store_true');parser.add_argument('--publish',action='store_true');parser.add_argument('--output',type=Path);args=parser.parse_args()
    self_test();data=StudyData(args.session)
    if args.inspect:
        print(json.dumps(clean_json(dict(session=data.sid,start=data.start,duration=data.duration,quality=data.quality,insertion_verdicts=dict(total=int(data.is_insertion.sum()),wrong=int((data.is_insertion&data.is_wrong).sum()),right=int((data.is_insertion&data.is_right).sum())),episodes=[{k:v for k,v in data.feature(float(a['start_s']),float(a['end_s'])).items() if k not in ('transitions','top_objects')} for a in data.annotations])),indent=2),flush=True)
    else:
        result=run_analysis(data)
        if args.publish:publish(result,args.output)
