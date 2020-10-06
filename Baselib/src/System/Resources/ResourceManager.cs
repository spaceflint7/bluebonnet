
namespace system.resources
{

    public class ResourceManager
    {

        public ResourceManager(string baseName, System.Reflection.Assembly assembly)
        {
            Console.WriteLine($"Creating ResourceManager with base name '{baseName}' in assembly '{assembly}'");
        }

        public string GetString(string name, System.Globalization.CultureInfo culture) => name;

    }

}
