using System;
using SCIA.OpenAPI;

namespace Scia.OpenAPI.Wpf
{
    public class OpenApiSimpleExampleContext
    {
        public string ConcreteGrade { get; set; } = "C20/25";
        public string SteelGrade { get; set; } ="S 235" ;
        public double A { get; set; } =5 ;
        public double B { get; set; } =4;
        public double C { get; set; } =3;
        public double LoadValue { get; set; } =-12000;
        public double SlabThickness { get; set; } =0.25;
        public string SteelProfile { get; set; } ="HEA260";

        public string BeamName { get; set; } = "ADM_MEMBER_001";
        public Guid Lc1_Id { get; set; } = Guid.NewGuid();
        public string SlabName { get; set; } = "SLAB_1";
        public ApiGuid Combi1 { get; set; } = ApiGuid.NewGuid();
       
    }
}
