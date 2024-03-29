﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MusicHub.Data.Models
{
    public class Album
    {
        public int Id { get; set; }

        [Required]
        [MinLength(3), MaxLength(40)]
        public string Name { get; set; }

        public DateTime ReleaseDate { get; set; }

        public decimal Price
            => this.Songs.Sum(p => p.Price);

        public int? ProducerId { get; set; }
        public Producer Producer { get; set; }

        public ICollection<Song> Songs { get; set; }
               = new HashSet<Song>();

    }
}
