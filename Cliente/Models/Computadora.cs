using System;
using System.Collections.Generic;
using System.Text;

namespace Cliente.Models
{
    public class Computadora
    {
        
        public string PC { get; set; } = null!;
        public string LAB { get; set; } = null!;

        public string Identificador { get; set; } = null!;

        public bool RegistradaEnELServidor { get; set; }

        public string IpServidor { get; set; }
    }
}
