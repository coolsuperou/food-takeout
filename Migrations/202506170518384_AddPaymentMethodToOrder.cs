namespace food_takeout.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPaymentMethodToOrder : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Order", "PaymentMethod", c => c.String(maxLength: 50));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Order", "PaymentMethod");
        }
    }
}
