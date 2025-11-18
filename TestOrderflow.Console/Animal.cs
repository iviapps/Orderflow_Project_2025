using System.Numerics;
using System.Runtime.CompilerServices;

namespace TestOrderflow.Console
{
    internal class Animal : object, IAnimal 
    {
        
        public string? Name { get; set; }
        public int Age { get; set; }

        public Animal() { }
        public Animal(string name, int age)
        {
            Name = name;
            Age = age;
        }
        public void Speak()
        {
            System.Console.WriteLine($"Hello, my name is {Name} and I am {Age} years old.");
        }


    }

    interface IAnimal
    {
        void Speak();
    }
    //static solo puede existir una instancia de esa clase en ejecución en todo el programa<- doble encapsulado, todos los metodos de una clase estatica son estaticos
    internal static class ExtensionsAnimal
    {
        public static void Arañar(this IAnimal nombre)
        {
            System.Console.WriteLine("El animal está arañando.");
        }

    }

    //record es inmutable, no se puede cambiar sus propiedades despues de creado el objeto
    public record Dog(string Name, int Age)
    {


    }
}
