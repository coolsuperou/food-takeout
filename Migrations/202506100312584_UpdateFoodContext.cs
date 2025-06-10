namespace food_takeout.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateFoodContext : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Restaurant", "ImageUrl", c => c.String(maxLength: 255));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Restaurant", "ImageUrl");
        }
    }
}
