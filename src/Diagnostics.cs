using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace KinetixStudio
{
    [DataContract]
    public class Diagnostics
    {
        [DataMember]
        public string Version, DataDirectory, SelectedProfileId, Driver;
        [DataMember]
        public int Profiles;
        public static void Write(string path)
        {
            Profile selected = Store.Current();
            Diagnostics result = new Diagnostics
            {
                Version = Brand.Version,
                DataDirectory = Store.Root,
                SelectedProfileId = selected.Id,
                Profiles = Store.All().Count
            };
            IntPtr client = IntPtr.Zero;
            try
            {
                client = Native.vigem_alloc();
                if (client == IntPtr.Zero)
                    throw new InvalidOperationException("Não foi possível reservar o cliente.");
                uint error = Native.vigem_connect(client);
                result.Driver = error == Native.OK ? "connected" : "0x" + error.ToString("X8");
            }
            catch (Exception ex)
            {
                result.Driver = ex.GetType().Name + ": " + ex.Message;
            }
            finally
            {
                if (client != IntPtr.Zero)
                {
                    Native.vigem_disconnect(client);
                    Native.vigem_free(client);
                }
            }

            using (FileStream output = File.Create(path))
                new DataContractJsonSerializer(typeof(Diagnostics)).WriteObject(output, result);
        }
    }
}
