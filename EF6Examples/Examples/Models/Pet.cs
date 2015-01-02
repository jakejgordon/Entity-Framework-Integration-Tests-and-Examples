using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Models
{
    public class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int OwningPersonId { get; set; }
        public int? FavoritePetFoodBrandId { get; set; }

        [ForeignKey("OwningPersonId")]
        public virtual Person Owner { get; set; }
        [ForeignKey("FavoritePetFoodBrandId")]
        public virtual PetFoodBrand FavoritePetFoodBrand { get; set; }
    }
}
