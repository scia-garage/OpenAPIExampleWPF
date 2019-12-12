using System;
using System.Collections.Generic;
using Results64Enums;
using SCIA.OpenAPI;
using SCIA.OpenAPI.OpenAPIEnums;
using SCIA.OpenAPI.Results;
using SCIA.OpenAPI.StructureModelDefinition;
using SCIA.OpenAPI.Utils;

namespace Scia.OpenAPI.Wpf
{
    /// <summary>
    /// Operation for working with model and results
    /// </summary>
    public static class OpenAPISimpleExample
    {
        public static void CreateModel(Structure model, OpenApiSimpleExampleContext context)
        {
            
           Guid comatid = Guid.NewGuid();
           model.CreateMaterial(new Material(comatid, "conc", 0, context.ConcreteGrade));
           Guid stmatid = Guid.NewGuid();
           model.CreateMaterial(new Material(stmatid, "steel", 1,context.SteelGrade));
            Guid css_steel = Guid.NewGuid();
            
           model.CreateCrossSection(new CrossSectionManufactured(css_steel, "steel.HEA", stmatid, context.SteelProfile, 1, 0));
          
            Guid n1 = Guid.NewGuid();
            Guid n2 = Guid.NewGuid();
            Guid n3 = Guid.NewGuid();
            Guid n4 = Guid.NewGuid();
            Guid n5 = Guid.NewGuid();
            Guid n6 = Guid.NewGuid();
            Guid n7 = Guid.NewGuid();
            Guid n8 = Guid.NewGuid();
            //Create structural nodes in local ADM
           model.CreateNode(new StructNode(n1, "n1", 0, 0, 0));
           model.CreateNode(new StructNode(n2, "n2", context.A, 0, 0));
           model.CreateNode(new StructNode(n3, "n3", context.A, context.B, 0));
           model.CreateNode(new StructNode(n4, "n4", 0, context.B, 0));
           model.CreateNode(new StructNode(n5, "n5", 0, 0, context.C));
           model.CreateNode(new StructNode(n6, "n6", context.A, 0, context.C));
           model.CreateNode(new StructNode(n7, "n7", context.A, context.B, context.C));
           model.CreateNode(new StructNode(n8, "n8", 0, context.B, context.C));

            Guid b1 = Guid.NewGuid();
            Guid b2 = Guid.NewGuid();
            Guid b3 = Guid.NewGuid();
            Guid b4 = Guid.NewGuid();
            //Create beams in local ADM
           model.CreateBeam(new Beam(b1, context.BeamName, css_steel, new Guid[2] { n1, n5 }));
           model.CreateBeam(new Beam(b2, "b2", css_steel, new Guid[2] { n2, n6 }));
           model.CreateBeam(new Beam(b3, "b3", css_steel, new Guid[2] { n3, n7 }));
           model.CreateBeam(new Beam(b4, "b4", css_steel, new Guid[2] { n4, n8 }));
            //Create fix nodal support in local ADM
            var Su1 = new PointSupport(Guid.NewGuid(), "Su1", n1)
            {
                ConstraintRx = eConstraintType.Free,
                ConstraintRy = eConstraintType.Free,
                ConstraintRz = eConstraintType.Free
            };
           model.CreatePointSupport(Su1);
           model.CreatePointSupport(new PointSupport(Guid.NewGuid(), "Su2", n2));
           model.CreatePointSupport(new PointSupport(Guid.NewGuid(), "Su3", n3));
           model.CreatePointSupport(new PointSupport(Guid.NewGuid(), "Su4", n4));

            Guid s1 = Guid.NewGuid();
            Guid[] nodes = new Guid[4] { n5, n6, n7, n8 };
            //Create flat slab in local ADM
            model.CreateSlab(new Slab(s1, context.SlabName, 0, comatid, context.SlabThickness, nodes));



            Guid lg1 = Guid.NewGuid();
            //Create load group in local ADM
           model.CreateLoadGroup(new LoadGroup(lg1, "lg1", 0));

            
            //Create load case in local ADM
           model.CreateLoadCase(new LoadCase(context.Lc1_Id, "lc1", 0, lg1, 1));

            //Combination
            CombinationItem[] combinationItems = new CombinationItem[1] { new CombinationItem(context.Lc1_Id, 1.5) };
            Combination C1 = new Combination(context.Combi1, "C1", combinationItems)
            {
                Category = eLoadCaseCombinationCategory.AccordingNationalStandard,
                NationalStandard = eLoadCaseCombinationStandard.EnUlsSetC
            };
           model.CreateCombination(C1);

            Guid sf1 = Guid.NewGuid();
            Console.WriteLine($"Set value of surface load on the slab: ");
            
            //Create surface load on slab in local ADM
           model.CreateSurfaceLoad(new SurfaceLoad(sf1, "sf1", context.LoadValue, context.Lc1_Id, s1, 2));
            // line support on B1
            var lineSupport = new LineSupport(Guid.NewGuid(), "lineSupport", b1)
            {
                Member = b1,
                ConstraintRx = eConstraintType.Free,
                ConstraintRy = eConstraintType.Free,
                ConstraintRz = eConstraintType.Free
            };
           model.CreateLineSupport(lineSupport);
            //line load on B1
            var lineLoad = new LineLoadOnBeam(Guid.NewGuid(), "lineLoad")
            {
                Member = b1,
                LoadCase = context.Lc1_Id,
                Value1 = -12500,
                Value2 = -12500,
                Direction = eDirection.X
            };
           model.CreateLineLoad(lineLoad);
        }

        public static OpenApiE2EResults ReadResults(Structure model, OpenApiSimpleExampleContext context)
        {
            OpenApiE2EResults storage = new OpenApiE2EResults();
            ResultsAPI resultsApi = model.InitializeResultsAPI();
            if (resultsApi == null)
            {
                return storage;
            }
            {
                OpenApiE2EResult beamForLc = new OpenApiE2EResult("beamInnerForces")
                {
                    ResultKey = new ResultKey
                    {
                        EntityType = eDsElementType.eDsElementType_Beam,
                        EntityName = context.BeamName,
                        CaseType = eDsElementType.eDsElementType_LoadCase,
                        CaseId = context.Lc1_Id,
                        Dimension = eDimension.eDim_1D,
                        ResultType = eResultType.eFemBeamInnerForces,
                        CoordSystem = eCoordSystem.eCoordSys_Local
                    }
                };
                beamForLc.Result = resultsApi.LoadResult(beamForLc.ResultKey);
                storage.SetResult(beamForLc);
            }
            {
                OpenApiE2EResult beamInnerForcesCombi = new OpenApiE2EResult("beamInnerForcesCombi")
                {
                    ResultKey = new ResultKey
                    {
                        EntityType = eDsElementType.eDsElementType_Beam,
                        EntityName = context.BeamName,
                        CaseType = eDsElementType.eDsElementType_Combination,
                        CaseId = context.Combi1,
                        Dimension = eDimension.eDim_1D,
                        ResultType = eResultType.eFemBeamInnerForces,
                        CoordSystem = eCoordSystem.eCoordSys_Local
                    }
                };
                beamInnerForcesCombi.Result = resultsApi.LoadResult(beamInnerForcesCombi.ResultKey);
                storage.SetResult(beamInnerForcesCombi);
            }
            {
                OpenApiE2EResult slabInnerForces = new OpenApiE2EResult("slabInnerForces")
                {
                    ResultKey = new ResultKey
                    {
                        EntityType = eDsElementType.eDsElementType_Slab,
                        EntityName = context.SlabName,
                        CaseType = eDsElementType.eDsElementType_LoadCase,
                        CaseId = context.Lc1_Id,
                        Dimension = eDimension.eDim_2D,
                        ResultType = eResultType.eFemInnerForces,
                        CoordSystem = eCoordSystem.eCoordSys_Local
                    }
                };
                slabInnerForces.Result = resultsApi.LoadResult(slabInnerForces.ResultKey);
                storage.SetResult(slabInnerForces);
            }
            {
                OpenApiE2EResult slabDeformations = new OpenApiE2EResult("slabDeformations")
                {
                    ResultKey = new ResultKey
                    {
                        EntityType = eDsElementType.eDsElementType_Slab,
                        EntityName = context.SlabName,
                        CaseType = eDsElementType.eDsElementType_LoadCase,
                        CaseId = context.Lc1_Id,
                        Dimension = eDimension.eDim_2D,
                        ResultType = eResultType.eFemDeformations,
                        CoordSystem = eCoordSystem.eCoordSys_Local
                    }
                };
                slabDeformations.Result = resultsApi.LoadResult(slabDeformations.ResultKey);
                storage.SetResult(slabDeformations);
            }
           
            return storage;
        }
    }
}
