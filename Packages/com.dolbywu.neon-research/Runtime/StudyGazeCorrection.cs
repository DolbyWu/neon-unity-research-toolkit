using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonResearch
{
    /// <summary>Small rigid direction-only correction in tracked-head local coordinates. No origin/scale/depth fitting.</summary>
    public static class StudyGazeCorrection
    {
        [Serializable] public sealed class Model
        {
            public string schema="gazeflow-local-ray-correction-v1",id,session_id,calibration_id,training_verification_id;
            public string correction_kind="fitted_rotation";
            public float horizontal_degrees,vertical_degrees;
            public double? applied_session_s;
            public string method="Equal-target-weight Wahba/Davenport rotation; head-local direction only; independent live verification required";
            public string limitation="No translation, depth, distortion or time correction; not a replacement for official mount calibration";
            public float[] quaternion_xyzw;
            public float angle_degrees;
            [Newtonsoft.Json.JsonIgnore] public Quaternion Rotation
            {
                get
                {
                    if(quaternion_xyzw==null||quaternion_xyzw.Length!=4)throw new InvalidOperationException("Invalid correction quaternion.");
                    var q=new Quaternion(quaternion_xyzw[0],quaternion_xyzw[1],quaternion_xyzw[2],quaternion_xyzw[3]);
                    float norm=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;
                    if(!Finite(norm)||Mathf.Abs(norm-1)>.001f||Quaternion.Angle(Quaternion.identity,q)>12.001f)throw new InvalidOperationException("Unsafe correction rotation.");
                    return q;
                }
            }
        }
        /// <summary>Operator nudge in head-local coordinates: positive horizontal is right, vertical is up. Not a fitted accuracy claim.</summary>
        public static Quaternion ManualRotation(float horizontalDegrees,float verticalDegrees)
        {
            if(!Finite(horizontalDegrees)||!Finite(verticalDegrees)||Mathf.Abs(horizontalDegrees)>12||Mathf.Abs(verticalDegrees)>12)throw new ArgumentException("Manual offsets must be finite and within 12 degrees.");
            var rotation=Quaternion.Euler(-verticalDegrees,horizontalDegrees,0);
            if(Quaternion.Angle(Quaternion.identity,rotation)>12.001f)throw new ArgumentException("Combined manual rotation exceeds 12 degrees. Inspect wearing and official mount settings.");
            return rotation;
        }
        // Davenport symmetric eigenproblem, scalar-first quaternion. Reference: NASA NTRS 19990104598.
        public static Quaternion Fit(IReadOnlyList<Vector3> observed,IReadOnlyList<Vector3> expected,float maximumDegrees=12)
            => FitInternal(observed,expected,maximumDegrees,5);
        public static Quaternion FitThreeTargets(IReadOnlyList<Vector3> observed,IReadOnlyList<Vector3> expected,float maximumDegrees=12)
        {
            if(observed==null||observed.Count!=3||expected==null||expected.Count!=3)throw new ArgumentException("Exactly three complete authored targets are required.");
            return FitInternal(observed,expected,maximumDegrees,3);
        }
        /// <summary>Explicit two-parameter tutorial model. Roll is fixed at zero, not estimated from a narrow pattern.</summary>
        public static Quaternion FitThreeTargetYawPitch(IReadOnlyList<Vector3> observed,IReadOnlyList<Vector3> expected,float maximumDegrees=12)
        {
            if(observed==null||expected==null||observed.Count!=3||expected.Count!=3||!Finite(maximumDegrees)||maximumDegrees<=0||maximumDegrees>12)throw new ArgumentException("Three complete targets and a rotation limit up to 12 degrees are required.");
            var a=new Vector3[3];var b=new Vector3[3];
            for(int i=0;i<3;i++){
                if(!StudyGazeTargetLog.Finite(observed[i])||!StudyGazeTargetLog.Finite(expected[i])||observed[i].sqrMagnitude<.01f||expected[i].sqrMagnitude<.01f)throw new ArgumentException("Missing/nonfinite direction pair.");
                a[i]=observed[i].normalized;b[i]=expected[i].normalized;
                if(a[i].z<.5f||b[i].z<.5f)throw new ArgumentException("Tutorial yaw/pitch fitting requires targets in the forward head-local field.");
            }
            float spreadA=0,spreadB=0;
            for(int i=0;i<3;i++)for(int j=i+1;j<3;j++){spreadA=Mathf.Max(spreadA,Vector3.Angle(a[i],a[j]));spreadB=Mathf.Max(spreadB,Vector3.Angle(b[i],b[j]));}
            if(spreadA<1||spreadB<1)throw new ArgumentException("Keep the head steady and look with the eyes at the three distinct cues; direction spread is below 1 degree.");
            double Loss(float pitch,float yaw){var q=Quaternion.Euler(pitch,yaw,0);double loss=0;for(int i=0;i<3;i++){var d=q*a[i]-b[i];loss+=d.sqrMagnitude;}return loss;}
            float p=0,y=0;double best=Loss(p,y);
            for(float step=4;step>=.0005f;step*=.5f)for(int iteration=0;iteration<100;iteration++){
                float nextP=p,nextY=y;double nextLoss=best;
                for(int axis=0;axis<2;axis++)for(int sign=-1;sign<=1;sign+=2){float cp=p+(axis==0?sign*step:0),cy=y+(axis==1?sign*step:0);double loss=Loss(cp,cy);if(loss<nextLoss){nextP=cp;nextY=cy;nextLoss=loss;}}
                if(nextLoss>=best)break;p=nextP;y=nextY;best=nextLoss;
            }
            var result=Quaternion.Euler(p,y,0);
            if(Quaternion.Angle(Quaternion.identity,result)>maximumDegrees)throw new InvalidOperationException("Required yaw/pitch correction exceeds the safety limit. Repeat official mount calibration.");
            return result;
        }
        private static Quaternion FitInternal(IReadOnlyList<Vector3> observed,IReadOnlyList<Vector3> expected,float maximumDegrees,int minimum)
        {
            if(observed==null||expected==null||observed.Count!=expected.Count||observed.Count<minimum)throw new ArgumentException("Insufficient complete direction pairs.");
            if(!Finite(maximumDegrees)||maximumDegrees<=0||maximumDegrees>12)throw new ArgumentException("Invalid rotation limit.");
            var a=new Vector3[observed.Count];var b=new Vector3[a.Length];var m=new double[3,3];
            for(int n=0;n<a.Length;n++)
            {
                if(!StudyGazeTargetLog.Finite(observed[n])||!StudyGazeTargetLog.Finite(expected[n])||observed[n].sqrMagnitude<.01f||expected[n].sqrMagnitude<.01f)throw new ArgumentException("Missing/nonfinite direction pair.");
                a[n]=observed[n].normalized;b[n]=expected[n].normalized;
                for(int i=0;i<3;i++)for(int j=0;j<3;j++)m[i,j]+=(double)a[n][i]*b[n][j];
            }
            float areaA=0,areaB=0;
            for(int i=1;i<a.Length;i++)for(int j=i+1;j<a.Length;j++)
            {areaA=Mathf.Max(areaA,Vector3.Cross(a[i]-a[0],a[j]-a[0]).magnitude);areaB=Mathf.Max(areaB,Vector3.Cross(b[i]-b[0],b[j]-b[0]).magnitude);}
            if(areaA<.005f||areaB<.005f)throw new ArgumentException("Directions do not span a two-dimensional target pattern.");
            double trace=m[0,0]+m[1,1]+m[2,2];var k=new double[4,4];k[0,0]=trace;
            k[0,1]=k[1,0]=m[1,2]-m[2,1];k[0,2]=k[2,0]=m[2,0]-m[0,2];k[0,3]=k[3,0]=m[0,1]-m[1,0];
            for(int i=0;i<3;i++)for(int j=0;j<3;j++)k[i+1,j+1]=m[i,j]+m[j,i]-(i==j?trace:0);
            // Shift all eigenvalues positive; power iteration then selects the largest algebraic eigenvalue.
            for(int i=0;i<4;i++)k[i,i]+=a.Length+1;
            var v=new[]{1d,0d,0d,0d};
            for(int step=0;step<4096;step++)
            {
                var next=new double[4];double norm=0;
                for(int i=0;i<4;i++){for(int j=0;j<4;j++)next[i]+=k[i,j]*v[j];norm+=next[i]*next[i];}
                norm=Math.Sqrt(norm);if(norm<1e-12)throw new InvalidOperationException("Rotation fit is singular.");
                for(int i=0;i<4;i++)v[i]=next[i]/norm;
            }
            var result=new Quaternion((float)v[1],(float)v[2],(float)v[3],(float)v[0]).normalized;
            if(Quaternion.Angle(Quaternion.identity,result)>maximumDegrees)throw new InvalidOperationException("Required correction is too large. Check headset/mount/head binding and repeat official calibration.");
            return result;
        }
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
