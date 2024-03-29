﻿using System.ComponentModel.DataAnnotations;

namespace API_Peliculas.Modelos.Dtos
{
    //9º
    public class CrearCategoriaDto
    {
        //Esta validación es importante, sino se crea sin nombre la categoría
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(100, ErrorMessage = "El número máximo de caracteres es de 100")]
        public string Nombre { get; set; }
    }
}
