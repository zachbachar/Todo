namespace CityShob.ToDo.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Tags",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TodoItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false, maxLength: 100),
                        IsCompleted = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                        DueDate = c.DateTime(),
                        Priority = c.Int(nullable: false),
                        LockedByConnectionId = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TodoItemTags",
                c => new
                    {
                        TodoItem_Id = c.Int(nullable: false),
                        Tag_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.TodoItem_Id, t.Tag_Id })
                .ForeignKey("dbo.TodoItems", t => t.TodoItem_Id, cascadeDelete: true)
                .ForeignKey("dbo.Tags", t => t.Tag_Id, cascadeDelete: true)
                .Index(t => t.TodoItem_Id)
                .Index(t => t.Tag_Id);

            CreateIndex("dbo.Tags", "Name", unique: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TodoItemTags", "Tag_Id", "dbo.Tags");
            DropForeignKey("dbo.TodoItemTags", "TodoItem_Id", "dbo.TodoItems");
            DropIndex("dbo.TodoItemTags", new[] { "Tag_Id" });
            DropIndex("dbo.TodoItemTags", new[] { "TodoItem_Id" });
            DropTable("dbo.TodoItemTags");
            DropTable("dbo.TodoItems");
            DropTable("dbo.Tags");
        }
    }
}
