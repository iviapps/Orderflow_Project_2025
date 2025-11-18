// See https://aka.ms/new-console-template for more information
using TestOrderflow.Console;

Console.WriteLine("Hello, World!");

var animal1 = new TestOrderflow.Console.Animal("Firulais", 3);
var animal2 = new TestOrderflow.Console.Animal("perro", 3);
//se almaacena en dos espacios de memoria distintos por eso no son iguales <- equals compara el espacio en memoria. 
//Sin emabrgo EQUALS con RECORDs compara las propiedades internas <- true 
animal1.Equals(animal2);
animal1.Arañar();

//Es false porque son dos instancias distintas en memoria.
Console.WriteLine(animal1.Equals(animal2));

var dog1 = new Dog("Firulais", 3);
var dog2 = new Dog("Firulais", 3);
//Ahora es true porque compara las propiedades internas. 
Console.WriteLine(dog1.Equals(dog2));