namespace Examples.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SetFavoriteFoodBrandOnFluffy : DbMigration
    {
        public override void Up()
        {
            this.Sql("INSERT INTO PetFoodBrand (BrandName) VALUES ('Crude Animal Fiber Specialties') ");
            this.Sql("UPDATE Pet SET FavoritePetFoodBrandId = 1 WHERE Name = 'Fluffy' ");
        }
        
        public override void Down()
        {
        }
    }
}
