using System;

namespace CodeExamine
{
    public class Greeter
    {
        public void Greet()
        {
            var message = "Hello, Roslyn!";
            PrintMessage(message);

            var now = DateTime.Now;
            now.
        }

        private void PrintMessage(string message)
        {
            var length = message.Length;
            Console.WriteLine("{1} {0}", message, length);
        }
    }
}
