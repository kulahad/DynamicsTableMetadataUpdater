using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Dynamics.AX.Metadata.Storage;
using Microsoft.Dynamics.AX.Metadata.Storage.Runtime;
using Microsoft.Dynamics.AX.Metadata.Providers;
using Microsoft.Dynamics.AX.Metadata.MetaModel;
using Microsoft.Dynamics.ApplicationPlatform.Environment;
using CsvHelper;
using System.Globalization;
using System.IO;

namespace AOTTableViewerNetFramework
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var modelName = "Sable37 EDI";

            //Get Environment details
            var environment = EnvironmentFactory.GetApplicationEnvironment();
            var packageDir = environment.Aos.PackageDirectory;

            //Get Providers
            var runtimeProviderConfiguration = new RuntimeProviderConfiguration(packageDir);
            var metadataProviderFactory = new MetadataProviderFactory();

            //runtime provider - fails to update
            IMetadataProvider provider = metadataProviderFactory.CreateRuntimeProvider(runtimeProviderConfiguration);

            //runtime provider - can update metadata 
            IMetadataProvider diskMetadataProvider = new MetadataProviderFactory().CreateDiskProvider(packageDir);

            foreach (var model in diskMetadataProvider.ModelManifest.ListModels())
            {
                Console.WriteLine($"model - {model}");
            }

            //Get list of tables from the model
            List<string> tablesList = new List<string>(provider.Tables.ListObjectsForModel(modelName));
            List<TableMetadata> csvTables = new List<TableMetadata>();

            foreach (var table in tablesList)
            {
                AxTable tmpTable = provider.Tables.Read(table);
                Console.WriteLine($"{tmpTable.Name} - {tmpTable.TableGroup}");
                csvTables.Add(new TableMetadata { TableName = tmpTable.Name, TableGroup = tmpTable.TableGroup.ToString(), TableType = tmpTable.TableType.ToString() });
            }


            var path = $".\\{modelName}TableDetails.csv";
            using (var writer = new StreamWriter(path))
            {

                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(csvTables);
                }
            }
            //AxTable DFUConnection_AzureBlobStaging = provider.Tables.Read("DFUConnection_AzureBlobStaging");
            //DFUConnection_AzureBlobStaging.TitleField1 = "Ahad from C# code";
            //ModelSaveInfo modelSaveInfo = diskMetadataProvider.ModelManifest.ConstructSaveInfo(diskMetadataProvider.ModelManifest.GetMetadataReferenceFromObject("DXC Finance Utilities", "AxTable", DFUConnection_AzureBlobStaging.Name).ModelReference);
            //diskMetadataProvider.Tables.Update(DFUConnection_AzureBlobStaging, modelSaveInfo);


            
            Console.ReadLine();


        }
    }
}
