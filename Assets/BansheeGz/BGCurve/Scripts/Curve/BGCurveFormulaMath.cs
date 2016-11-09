using System;
using UnityEngine;


namespace BansheeGz.BGSpline.Curve
{
    /// <summary>  
    /// Do not use it. This is an old deprecated math, using parametric formulas to get position and tangent.  
    /// It does not have any advantage over BGCurveBaseMath, which is slightly faster and have a little lesser memory footprint.
    /// We leave this class for testing and reference purposes only.
    /// </summary>
    [Obsolete("Use BGCurveBaseMath. This class is for testing purpose only")]
    public class BGCurveFormulaMath : BGCurveBaseMath
    {
        // cached data (t=some ratio [0,1], tr=1-t, t2=t*t, etc.).
        //position
        private float[] bakedT;
        private float[] bakedT2;
        private float[] bakedTr2;
        private float[] bakedT3;
        private float[] bakedTr3;
        private float[] bakedTr2xTx3;
        private float[] bakedT2xTrx3;
        private float[] bakedTxTrx2;

        //tangent related
        private float[] bakedTr2x3;
        private float[] bakedTxTrx6;
        private float[] bakedT2x3;
        private float[] bakedTx2;
        private float[] bakedTrx2;

        public BGCurveFormulaMath(BGCurve curve, Config config)
            : base(curve, config)
        {
        }

        //some additional init
        protected override void AfterInit(Config config)
        {
            //let's bake some data
            var parts = config.Parts;
            var sectionPointsCount = parts + 1;

            Array.Resize(ref bakedT, sectionPointsCount);

            Array.Resize(ref bakedT2, sectionPointsCount);
            Array.Resize(ref bakedTr2, sectionPointsCount);
            Array.Resize(ref bakedT3, sectionPointsCount);
            Array.Resize(ref bakedTr3, sectionPointsCount);

            Array.Resize(ref bakedTr2xTx3, sectionPointsCount);
            Array.Resize(ref bakedT2xTrx3, sectionPointsCount);
            Array.Resize(ref bakedTxTrx2, sectionPointsCount);

            if (NeedTangentFormula)
            {
                Array.Resize(ref bakedTr2x3, sectionPointsCount);
                Array.Resize(ref bakedTxTrx6, sectionPointsCount);
                Array.Resize(ref bakedT2x3, sectionPointsCount);
                Array.Resize(ref bakedTx2, sectionPointsCount);
                Array.Resize(ref bakedTrx2, sectionPointsCount);
            }

            for (var i = 0; i <= parts; i++)
            {
                var t = i/(float) parts;
                var tr = 1 - t;
                var t2 = t*t;
                var tr2 = tr*tr;

                bakedT[i] = t;

                bakedT2[i] = t2;
                bakedTr2[i] = tr2;
                bakedT3[i] = t2*t;
                bakedTr3[i] = tr2*tr;

                bakedTr2xTx3[i] = 3*tr2*t;
                bakedT2xTrx3[i] = 3*tr*t2;
                bakedTxTrx2[i] = 2*tr*t;

                if (!NeedTangentFormula) continue;


                bakedTr2x3[i] = 3*tr2;
                bakedTxTrx6[i] = 6*tr*t;
                bakedT2x3[i] = 3*t2;
                bakedTx2[i] = 2*t;
                bakedTrx2[i] = 2*tr;
            }
        }

        //standard c# dispose
        public override void Dispose()
        {
            base.Dispose();

            var emptyArray = new float[0];
            bakedT = emptyArray;
            bakedT2 = emptyArray;
            bakedTr2 = emptyArray;
            bakedT3 = emptyArray;
            bakedTr3 = emptyArray;
            bakedTr2xTx3 = emptyArray;
            bakedT2xTrx3 = emptyArray;
            bakedTxTrx2 = emptyArray;

            bakedTr2x3 = emptyArray;
            bakedTxTrx6 = emptyArray;
            bakedT2x3 = emptyArray;
            bakedTx2 = emptyArray;
            bakedTrx2 = emptyArray;
        }

        //calculate one split section data
        protected override void CalculateSplitSection(SectionInfo section, BGCurvePointI @from, BGCurvePointI to)
        {
            Resize(section.points, config.Parts + 1);

            //======================================== 
            //                    Calculate points
            //========================================
            //-----------section data
            var fromPos = section.OriginalFrom;
            var toPos = section.OriginalTo;
            var control1 = section.OriginalFromControl;
            var control2 = section.OriginalToControl;

            var controlFromAbsent = @from.ControlType == BGCurvePoint.ControlTypeEnum.Absent;
            var controlToAbsent = to.ControlType == BGCurvePoint.ControlTypeEnum.Absent;
            var noControls = controlFromAbsent && controlToAbsent;
            var bothControls = !controlFromAbsent && !controlToAbsent;
            if (!noControls && !bothControls && controlFromAbsent) control1 = control2;

            //snapping
            var snapIsOn = curve.SnapType == BGCurve.SnapTypeEnum.Curve;


            //-----------calc some data

            // no controls
            Vector3 fromTo = Vector3.zero, fromToTangentWorld = Vector3.zero;
            if (noControls)
            {
                fromTo = toPos - fromPos;
                if (cacheTangent) fromToTangentWorld = (to.PositionWorld - @from.PositionWorld).normalized;
            }

            // tangent related
            Vector3 control1MinusFrom = Vector3.zero, control2MinusControl1 = Vector3.zero, toMinusControl2 = Vector3.zero, toMinusControl1 = Vector3.zero;
            if (!config.UsePointPositionsToCalcTangents && cacheTangent)
            {
                control1MinusFrom = control1 - fromPos;
                if (bothControls)
                {
                    control2MinusControl1 = control2 - control1;
                    toMinusControl2 = toPos - control2;
                }
                else
                {
                    toMinusControl1 = toPos - control1;
                }
            }


            //-----------  Critical block starts
            var length = bakedT.Length;
            for (var i = 0; i < length; i++)
            {
                var point = section.points[i] ?? (section.points[i] = new SectionPointInfo());

                Vector3 pos;

                if (noControls)
                {
                    // =================  NoControls
                    // ---------- position 
                    var t = bakedT[i];
                    pos = new Vector3(fromPos.x + fromTo.x * t, fromPos.y + fromTo.y * t, fromPos.z + fromTo.z * t);

                    if (snapIsOn) curve.ApplySnapping(ref pos);

                    point.Position = pos;

                    //---------- tangents
                    if (cacheTangent) point.Tangent = fromToTangentWorld;
                }
                else
                {
                    // =================  At least One control
                    //---------- position  

                    if (bothControls)
                    {
                        var tr3 = bakedTr3[i];
                        var tr2xTx3 = bakedTr2xTx3[i];
                        var t2xTrx3 = bakedT2xTrx3[i];
                        var t3 = bakedT3[i];

                        pos = new Vector3(tr3 * fromPos.x + tr2xTx3 * control1.x + t2xTrx3 * control2.x + t3 * toPos.x,
                            tr3 * fromPos.y + tr2xTx3 * control1.y + t2xTrx3 * control2.y + t3 * toPos.y,
                            tr3 * fromPos.z + tr2xTx3 * control1.z + t2xTrx3 * control2.z + t3 * toPos.z);
                    }
                    else
                    {
                        var tr2 = bakedTr2[i];
                        var txTrx2 = bakedTxTrx2[i];
                        var t2 = bakedT2[i];

                        pos = new Vector3(tr2 * fromPos.x + txTrx2 * control1.x + t2 * toPos.x,
                            tr2 * fromPos.y + txTrx2 * control1.y + t2 * toPos.y,
                            tr2 * fromPos.z + txTrx2 * control1.z + t2 * toPos.z);
                    }

                    if (snapIsOn) curve.ApplySnapping(ref pos);

                    point.Position = pos;


                    //---------- tangents 
                    if (cacheTangent)
                    {
                        if (config.UsePointPositionsToCalcTangents)
                        {
                            //-------- Calc by point's positions
                            //we skip 1st point, cause we do not have enough info for it. we'll set it at the next step
                            if (i != 0)
                            {
                                var prevPoint = section[i - 1];

                                var prevPosition = prevPoint.Position;

                                var tangent = new Vector3(pos.x - prevPosition.x, pos.y - prevPosition.y, pos.z - prevPosition.z);
                                //Vector3.normalized inlined (tangent=tangent.normalized)
                                var marnitude = (float)Math.Sqrt((double)tangent.x * (double)tangent.x + (double)tangent.y * (double)tangent.y + (double)tangent.z * (double)tangent.z);
                                tangent = ((double)marnitude > 9.99999974737875E-06) ? new Vector3(tangent.x / marnitude, tangent.y / marnitude, tangent.z / marnitude) : Vector3.zero;

                                prevPoint.Tangent = tangent;

                                //we will adjust it later (if there is another section after this one , otherwise- no more data for more precise calculation)
                                if (i == config.Parts) point.Tangent = prevPoint.Tangent;
                            }
                        }
                        else
                        {
                            //-------- Calc by a formula
                            Vector3 tangent;
                            if (bothControls)
                            {
                                var tr2x3 = bakedTr2x3[i];
                                var txTrx6 = bakedTxTrx6[i];
                                var t2x3 = bakedT2x3[i];
                                tangent = new Vector3(tr2x3 * control1MinusFrom.x + txTrx6 * control2MinusControl1.x + t2x3 * toMinusControl2.x,
                                    tr2x3 * control1MinusFrom.y + txTrx6 * control2MinusControl1.y + t2x3 * toMinusControl2.y,
                                    tr2x3 * control1MinusFrom.z + txTrx6 * control2MinusControl1.z + t2x3 * toMinusControl2.z);
                            }
                            else
                            {
                                var trx2 = bakedTrx2[i];
                                var tx2 = bakedTx2[i];
                                tangent = new Vector3(trx2 * control1MinusFrom.x + tx2 * toMinusControl1.x,
                                    trx2 * control1MinusFrom.y + tx2 * toMinusControl1.y,
                                    trx2 * control1MinusFrom.z + tx2 * toMinusControl1.z);
                            }

                            //Vector3.normalized inlined (tangent=tangent.normalized)
                            var marnitude = (float)Math.Sqrt((double)tangent.x * (double)tangent.x + (double)tangent.y * (double)tangent.y + (double)tangent.z * (double)tangent.z);
                            tangent = ((double)marnitude > 9.99999974737875E-06) ? new Vector3(tangent.x / marnitude, tangent.y / marnitude, tangent.z / marnitude) : Vector3.zero;

                            // set tangent
                            point.Tangent = tangent;
                        }
                    }
                }


                if (i == 0) continue;

                // ---------- distance to section start (Vector3.Distance inlined)
                var prevPos = section[i - 1].Position;
                double x = pos.x - prevPos.x;
                double y = pos.y - prevPos.y;
                double z = pos.z - prevPos.z;
                point.DistanceToSectionStart = section[i - 1].DistanceToSectionStart + ((float)Math.Sqrt(x * x + y * y + z * z));
            }
            //-----------  Critical block ends
        }
    }
}