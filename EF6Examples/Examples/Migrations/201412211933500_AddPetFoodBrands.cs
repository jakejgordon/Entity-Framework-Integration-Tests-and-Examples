namespace Examples.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPetFoodBrands : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PetFoodBrand",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        BrandName = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.Pet", "FavoritePetFoodBrandId", c => c.Int());
            CreateIndex("dbo.Pet", "FavoritePetFoodBrandId");
            AddForeignKey("dbo.Pet", "FavoritePetFoodBrandId", "dbo.PetFoodBrand", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pet", "FavoritePetFoodBrandId", "dbo.PetFoodBrand");
            DropIndex("dbo.Pet", new[] { "FavoritePetFoodBrandId" });
            DropColumn("dbo.Pet", "FavoritePetFoodBrandId");
            DropTable("dbo.PetFoodBrand");
        }
    }
}
