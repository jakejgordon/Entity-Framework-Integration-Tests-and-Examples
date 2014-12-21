namespace Examples.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SetupInitialDatabase : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Person",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Pet",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        OwningPersonId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Person", t => t.OwningPersonId, cascadeDelete: true)
                .Index(t => t.OwningPersonId);

            this.Sql("INSERT INTO Person (Name) VALUES ('Jane')");
            this.Sql("INSERT INTO Person (Name) VALUES ('Bob')");
            this.Sql("INSERT INTO Person (Name) VALUES ('Anna')");
            this.Sql("INSERT INTO Pet (Name, OwningPersonId) VALUES ('Fluffy', 1)");
            this.Sql("INSERT INTO Pet (Name, OwningPersonId) VALUES ('Puffy', 1)");
            this.Sql("INSERT INTO Pet (Name, OwningPersonId) VALUES ('Fido', 2)");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pet", "OwningPersonId", "dbo.Person");
            DropIndex("dbo.Pet", new[] { "OwningPersonId" });
            DropTable("dbo.Pet");
            DropTable("dbo.Person");
        }
    }
}
