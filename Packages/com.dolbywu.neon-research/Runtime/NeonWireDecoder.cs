using System;

namespace NeonResearch
{
    /// <summary>Independent decoder of the documented big-endian Neon wire layouts.
    /// Not the SDK's converted coordinate representation. No physical accuracy claim.</summary>
    public static class NeonWireDecoder
    {
        public sealed class Datum
        {
            public bool supported, worn, binocular, eyeState, dualMonocular;
            public float x=float.NaN, y=float.NaN, leftPupil=float.NaN, rightPupil=float.NaN;
            public float leftX=float.NaN,leftY=float.NaN,rightX=float.NaN,rightY=float.NaN;
        }
        private static float F(byte[] bytes,int offset)
        {
            var value=new byte[4]; Buffer.BlockCopy(bytes,offset,value,0,4);
            if(BitConverter.IsLittleEndian)Array.Reverse(value);
            return BitConverter.ToSingle(value,0);
        }
        public static Datum Decode(byte[] bytes)
        {
            var d=new Datum(); if(bytes==null)return d;
            int n=bytes.Length;
            if(n!=9&&n!=17&&n!=25&&n!=65&&n!=89&&n!=105)return d;
            d.supported=true;d.worn=bytes[8]==255;d.binocular=n!=17;
            if(d.binocular){d.x=F(bytes,0);d.y=F(bytes,4);}
            d.eyeState=n==65||n==89||n==105;
            if(d.eyeState){d.leftPupil=F(bytes,9);d.rightPupil=F(bytes,37);}
            d.dualMonocular=n==17||n==25||n==105;
            if(n==17){d.leftX=F(bytes,0);d.leftY=F(bytes,4);d.rightX=F(bytes,9);d.rightY=F(bytes,13);}
            else if(d.dualMonocular){int i=n==25?9:89;d.leftX=F(bytes,i);d.leftY=F(bytes,i+4);d.rightX=F(bytes,i+8);d.rightY=F(bytes,i+12);}
            return d;
        }
    }
}
