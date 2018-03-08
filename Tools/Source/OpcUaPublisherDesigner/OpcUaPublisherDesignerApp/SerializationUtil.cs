using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace PublisherDesignerApp
{
    public class SerializationUtil
    {
        public static async Task<bool> SaveToJsonFile(string filePath, object objectToSave)
        {
            bool isSuccess = false;
            StorageFile file = null;
            StorageFolder baseFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            try
            {
                //string path = Path.Combine(baseFolder.Path, filePath);
                file = await baseFolder.CreateFileAsync(filePath, CreationCollisionOption.ReplaceExisting);
                if (file != null)
                {
                    string content = JsonConvert.SerializeObject(objectToSave);
                    await FileIO.WriteTextAsync(file, content);
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MoxaOilDataEmu LoadConfig Exception: " + ex.Message);
            }
            return isSuccess;
        }

        public static async Task<object> LoadFromJsonFile<T>(string filePath)
        {
            object config = null;
            StorageFile file = null;
            StorageFolder baseFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            try
            {
                if (filePath.StartsWith("ms-appx:"))
                {
                    //StorageFile appDefaultConfig = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/PredefinedServiceResources.json"));
                    //await appDefaultConfig.CopyAsync(localFolder);
                    file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(filePath));
                }
                else
                {
                    string path = Path.Combine(baseFolder.Path, filePath);
                    file = await StorageFile.GetFileFromPathAsync(path);
                    //file = await localFolder.GetFileAsync(filePath);
                }
                if (file != null)
                {
                    string content = await FileIO.ReadTextAsync(file);
                    config = JsonConvert.DeserializeObject<T>(content);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MoxaOilDataEmu LoadConfig Exception: " + ex.Message);
            }
            return config;
        }

    };
}