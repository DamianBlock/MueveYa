using System;
using System.Collections.Generic;
using System.Text;

namespace AppFletesMueve.Models
{
    public class VehiculoModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Imagen { get; set; }
        public decimal Precio { get; set; }
        public string Info { get; set; } // Ejemplo: "★ 4.8  ⏱ 7min"
    }
}

