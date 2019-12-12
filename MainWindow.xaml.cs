using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using SCIA.OpenAPI;
using SCIA.OpenAPI.Utils;
using Environment = System.Environment;

namespace Scia.OpenAPI.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string ConcreteGrade { get; set; } = "C20/25";
        private string SteelGrade { get; set; } = "S 235";
        private string SteelProfile{ get; set; } = "HEA260";
        private double A { get; set; } = 4;
        private double B { get; set; } = 5;
        private double C { get; set; } = 3;
        private double SlabThickness { get; set; } = 0.25;
        private double LoadValue { get; set; } = -12000;

        private string GetAppPath()
        {
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            return directory.Parent.FullName;
        }

        /// <summary>
        /// Path to Scia engineer
        /// </summary>
        private string SciaEngineerFullPath => GetAppPath();

        public MainWindow()
        {
            InitializeComponent();
            SciaOpenApiAssemblyResolve();
        }

        /// <summary>
        /// Assembly resolve method has to be call here
        /// </summary>
        private void SciaOpenApiAssemblyResolve()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string dllName = args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
                string dllFullPath = Path.Combine(SciaEngineerFullPath, dllName);
                if (!File.Exists(dllFullPath))
                {
                    //return null;
                    dllFullPath = Path.Combine(SciaEngineerFullPath, "OpenAPI_dll", dllName);
                }
                if (!File.Exists(dllFullPath)) {
                    return null;
                }
                return Assembly.LoadFrom(dllFullPath);
            };
        }


        /// <summary>
        /// Scia open API run example
        /// DO NOT PLAY WITH GUI ELEMENTS HERE.
        /// It is not done run on GUI main thread
        /// </summary>
        /// <param name="env">Data for from calculation</param>
        /// <returns></returns>
        private object SciaOpenApiWorker(SCIA.OpenAPI.Environment env)
        {
           
            env.RunSCIAEngineer(SCIA.OpenAPI.Environment.GuiMode.ShowWindowShow);
            SciaFileGetter fileGetter = new SciaFileGetter();
            var esaFile = fileGetter.PrepareBasicEmptyFile(env.AppTempPath);
            if (!File.Exists(esaFile))
            {
                throw new InvalidOperationException($"File from manifest resource is not created ! Temp: {env.AppTempPath}");
            }
            EsaProject project = env.OpenProject(esaFile);


            OpenApiSimpleExampleContext APIContext = new OpenApiSimpleExampleContext()
            {
                A = this.A,
                B = this.B,
                C = this.C,
                SlabThickness = this.SlabThickness,
                LoadValue = this.LoadValue,
                ConcreteGrade = this.ConcreteGrade,
                SteelGrade = this.SteelGrade,
                SteelProfile = this.SteelProfile
            };
           OpenAPISimpleExample.CreateModel(project.Model, APIContext);
            project.Model.RefreshModel_ToSCIAEngineer();
            project.RunCalculation();
            return OpenAPISimpleExample.ReadResults(project.Model, APIContext);
        }
      

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            RunButton.Visibility = Visibility.Collapsed;
            TaskProgress.Visibility = Visibility.Visible;
            RunButton.IsEnabled = false;
            TextBlockOpenApi.Text = "WORKING";
            LoadValue = ParseValueDOUBLE(tbLoadValue.Text);
            A = ParseValueDOUBLE(tbA.Text);
            B = ParseValueDOUBLE(tbB.Text);
            C = ParseValueDOUBLE(tbC.Text);
           ConcreteGrade = tbConcreteGrade.Text;
           SteelGrade = tbSteelGrade.Text;
            SlabThickness = ParseValueDOUBLE(tbSlabthickness.Text);
           SteelProfile = cbSteelProfile.Text;
            new TaskFactory().StartNew(async () =>
            {
                
                var context = await RunOpenAPINonBlockingMainThread();
                Dispatcher.Invoke(() => { RunOpenApiAfterButtonClick(context); });
            });
        }

        private void RunOpenApiAfterButtonClick(SciaOpenApiContext context)
        {
            if (context.Exception != null)
            {
                TextBlockOpenApi.Text = $"Exception -> {Environment.NewLine} {context.Exception}";
                return;
            }

            var data = context.Data as OpenApiE2EResults;
            if (data == null)
            {
                TextBlockOpenApi.Text = "SOMETHING IS WRONG NO DATA !";
                return;
            }

            TextBlockOpenApi.Text = "RESULTS";
            foreach (var item in data.GetAll())
            {
                TextBlockOpenApi.Text += item.Value.Result.GetTextOutput();
            }
            var slabDef = data.GetResult("slabDeformations").Result;
            if (slabDef != null)
            {
                double maxvalue = 0;
                double pivot;
                for (int i = 0; i < slabDef.GetMeshElementCount(); i++)
                {
                    pivot = slabDef.GetValue(2, i);
                    if (Math.Abs(pivot) > Math.Abs(maxvalue))
                    {
                        maxvalue = pivot;

                    }
                }
                TextBlockOpenApi.Text += Environment.NewLine;
                TextBlockOpenApi.Text += $"Maximal slab deformation is { maxvalue.ToString()}";
            }
                RunButton.IsEnabled = true;
            RunButton.Visibility = Visibility.Visible;
            TaskProgress.Visibility = Visibility.Collapsed;
        }

        private Task<SciaOpenApiContext> RunOpenAPINonBlockingMainThread()
        {                    
            return new TaskFactory().StartNew<SciaOpenApiContext>(RunOpenAPI);
        }

        private SciaOpenApiContext RunOpenAPI()
        {
            var context = new SciaOpenApiContext(SciaEngineerFullPath, SciaOpenApiWorker);
            SciaOpenApiUtils.RunSciaOpenApi(context);
            return context;
        }

       private int ParseValueINT(string ReadData)
        {
            bool check = int.TryParse(ReadData, out int Value);
            if (check == false)
            {
                MessageBox.Show("Neplatný vstup!\nProsím, vložte celé kladné číslo.\n\n");
            }
            return Value;
        }



        private double ParseValueDOUBLE(string ReadData)
        {
            bool check = double.TryParse(ReadData, out double Value);
            if (check == false)
            {
                if (ReadData.Contains("."))
                {
                    ReadData = ReadData.Replace(".", ",");
                }
                else
                {
                    ReadData = ReadData.Replace(",", ".");
                }
                check = double.TryParse(ReadData, out Value);
            }
            if (check == false )
            {
                MessageBox.Show("Neplatný vstup!\nProsím, vložte kladné číslo ve desetinném formátu.\n\n");
            }
            return Value;
        }
    }
}
