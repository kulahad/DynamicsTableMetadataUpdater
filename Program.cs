using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Dynamics.AX.Metadata.MetaModel;
using Microsoft.Dynamics.AX.Metadata.Providers;
using Microsoft.Dynamics.ApplicationPlatform.Environment;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Dynamics.AX.Metadata.Core.MetaModel;
using Microsoft.Dynamics.AX.Metadata.Storage;

namespace AOTTableViewerNetFramework
{
    class Program
    {
        static void Main(string[] args)
        {
            IMetadataProvider diskMetadataProvider = GetDiskMetadataProvider();

            //add list of models or just 1 model for export
            List<string> listOfModels = new List<string>
            {
                "Sable37 EDI",
            };

            int choice;
            do
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Menu:");
                Console.WriteLine("1. Save CSV file with metadata for the tablegroup for the model");
                Console.WriteLine("2. Update the metadata from CSV file");
                Console.WriteLine("3. View list of Models");
                Console.WriteLine("4. Exit");
                Console.Write("Enter your choice: ");

                if (!int.TryParse(Console.ReadLine(), out choice))
                {
                    Console.WriteLine("Invalid input. Please enter a number.");
                    continue;
                }

                switch (choice)
                {
                    case 1:
                        Console.Clear();
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("Saving CSV file with metadata to file...");
                        List<TableMetadataUpdate> csvTables = GetTablesFromModels(listOfModels, diskMetadataProvider);
                        ExportCsv(csvTables);
                        break;
                    case 2:
                        Console.Clear();
                        Console.ForegroundColor = ConsoleColor.Red;
                        DisplayModelsFromDiskProvider(diskMetadataProvider);

                        Console.WriteLine("Enter the model name for the update: ");
                        string modelName = Console.ReadLine();
                        List<TableMetadataUpdate> metadataUpdatesList = ImportCsv();
                        Console.WriteLine("Updating the metadata...");
                        UpdateTableMetadataUpdate(diskMetadataProvider, modelName, metadataUpdatesList);
                        break;
                    case 3:
                        Console.Clear();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("List of Models:");
                        DisplayModelsFromDiskProvider(diskMetadataProvider);
                        break;
                    case 4:
                        Console.WriteLine("Exiting program.");
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please select a valid option.");
                        break;
                }

            } while (choice != 4);
        }

        static IMetadataProvider GetDiskMetadataProvider()
        {
            var environment = EnvironmentFactory.GetApplicationEnvironment();
            var packageDir = environment.Aos.PackageDirectory;
            return new MetadataProviderFactory().CreateDiskProvider(packageDir);
        }

        static void DisplayModelsFromDiskProvider(IMetadataProvider diskMetadataProvider)
        {
            Console.WriteLine($"{diskMetadataProvider.ModelManifest.ListModels().Count} Model(s) available.");

            foreach (var model in diskMetadataProvider.ModelManifest.ListModels())
            {
                Console.WriteLine($"{model}");
            }
        }

        static List<TableMetadataUpdate> GetTablesFromModels(List<string> listOfModels, IMetadataProvider provider)
        {
            List<TableMetadataUpdate> csvTables = new List<TableMetadataUpdate>();
            foreach (var model in listOfModels)
            {
                List<string> tablesList = new List<string>(provider.Tables.ListObjectsForModel(model));
                foreach (var table in tablesList)
                {
                    AxTable tmpTable = provider.Tables.Read(table);
                    Console.WriteLine($"{tmpTable.Name} - {tmpTable.TableGroup}");
                    csvTables.Add(new TableMetadataUpdate { TableName = tmpTable.Name, TableGroup = tmpTable.TableGroup.ToString(), TableType = tmpTable.TableType.ToString() });
                }
            }
            return csvTables;
        }

        static void ExportCsv(List<TableMetadataUpdate> csvTables)
        {
            var path = $".\\TableGroupMetadata.csv";
            using (var writer = new StreamWriter(path))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(csvTables);
                }
            }
            Console.WriteLine($"CSV file saved to {path}");
        }

        static List<TableMetadataUpdate> ImportCsv()
        {
            List<TableMetadataUpdate> csvTables = new List<TableMetadataUpdate>();
            Console.WriteLine("Enter the path to the CSV file:");
            var path = Console.ReadLine();

            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
            { HasHeaderRecord = true, HeaderValidated = null, MissingFieldFound = null };

            Console.WriteLine($"Reading from {path}");
            using (var reader = new StreamReader(path))
            {
                using (var csv = new CsvReader(reader, config))
                {
                    while (csv.Read())
                    {
                        csvTables.Add(csv.GetRecord<TableMetadataUpdate>());
                    }
                }
            }
            Console.WriteLine($"Found {csvTables.Count} records");
            return csvTables;
        }

        static void UpdateTableMetadataUpdate(IMetadataProvider provider, string modelName, List<TableMetadataUpdate> metadataUpdatesList)
        {
            List<string> tablesList = new List<string>(provider.Tables.ListObjectsForModel(modelName));
            foreach (var table in tablesList)
            {
                AxTable tableInstance = provider.Tables.Read(table);
                var newGroup = metadataUpdatesList.Find(x => x.TableName == tableInstance.Name);

                if (newGroup != null && !string.IsNullOrEmpty(newGroup.TableGroupNew))
                {
                    tableInstance.TableGroup = (TableGroup)Enum.Parse(typeof(TableGroup), newGroup.TableGroupNew);

                    ModelSaveInfo modelSaveInfo = provider.ModelManifest.ConstructSaveInfo(provider.ModelManifest.GetMetadataReferenceFromObject(modelName, "AxTable", tableInstance.Name).ModelReference);
                    provider.Tables.Update(tableInstance, modelSaveInfo);
                }
            }

            Console.WriteLine("Metadata updated successfully.");
        }
    }
}