using System.Collections.Generic;
using AiCup22.Model;
using System;
using System.Linq;


namespace AiCup22
{
    public class MyStrategy
    {
        public static Constants constants;
        public MyStrategy(Constants constants) { MyStrategy.constants = constants; }
        public Order GetOrder(Game game, DebugInterface debugInterface)
        {

            if (game.CurrentTick == 0 || game.CurrentTick % 5 == 0) Arena.update(game);

            Arena.ListeningArena(game);

            var orders = new Dictionary<int, UnitOrder>();

            foreach (var teammate in Arena.Teams)
            {
                orders.Add(teammate.Id, Arena.GetOrderForTeammate(teammate));
            }

            return new Order(orders);

        }
        public void DebugUpdate(int displayedTick, DebugInterface debugInterface)
        {


        }
        public void Finish() { }


        public static class Arena
        {
            public static List<Unit> Teams;
            public static List<Unit> Enemies;
            public static List<Obstacle> Obstacles;
            public static List<Loot> ShieldPotions;
            public static List<Loot> Weapon;
            public static List<Loot> Ammo;

            public static int size;
            public static double dSize;
            public static int index;
            public static int unitRadius;
            public static Vec2 currentCenter;
            public static DebugInterface debugInterface;
            public static int tick;

            public static void update(Game game)
            {
                dSize = game.Zone.CurrentRadius;
                unitRadius = (int)MyStrategy.constants.UnitRadius;
                size = (int)game.Zone.CurrentRadius * 2;
                currentCenter = game.Zone.CurrentCenter;
                tick = game.CurrentTick;
            }

            public static void ListeningArena(Game game)
            {

                Teams = new List<Unit>();
                Enemies = new List<Unit>();
                Obstacles = new List<AiCup22.Model.Obstacle>();
                ShieldPotions = new List<Loot>();
                Weapon = new List<Loot>();
                Ammo = new List<Loot>();

                foreach (var obstacle in MyStrategy.constants.Obstacles)
                    if (!obstacle.CanShootThrough) Obstacles.Add(obstacle);

                foreach (var loot in game.Loot)
                {
                    if (loot.Item is Item.ShieldPotions)
                    {
                        ShieldPotions.Add(loot);
                    }
                    if (loot.Item is Item.Weapon)
                    {
                        Weapon.Add(loot);
                    }
                    if (loot.Item is Item.Ammo)
                    {
                        Ammo.Add(loot);
                    }
                }

                foreach (var unit in game.Units)
                {

                    if (unit.PlayerId == game.MyId)
                    {
                        Teams.Add(unit);

                    }
                    else
                    {

                        // debugInterface.AddPlacedText(unit.Position, "enвывфывфыв", new Model.Vec2(0, 0), 1.5, new AiCup22.Debugging.Color(0, 0, 1, 1));
                        //debugInterface.Add(new  Debugging.DebugData.PlacedText(unit.Position, "enвывфывфыв", new Model.Vec2(0, 0), 1.5, new AiCup22.Debugging.Color(0, 0, 1, 1)));
                        //debugInterface.Add(new Debugging.DebugData.Ring(unit.Position, 1.5, 0.2, new AiCup22.Debugging.Color(1, 0, 0, 1)));
                        Enemies.Add(unit);
                    }

                }

            }

            internal static UnitOrder GetOrderForTeammate(Unit unit)
            {

                var target_case = GetTargetNew(unit);

                return new UnitOrder(
                                new Vec2(target_case.velocity.x, target_case.velocity.y),
                                new Vec2(target_case.direction.x, target_case.direction.y),
                                target_case.action);

            }



            public static strategy GetTargetNew(Unit unit)
            {
                Vector vectorUnit = newVector(unit.Position);

                int weapon = (int)unit.Weapon;

                classStrategy targetStrategy = new classStrategy();

                targetStrategy.action = new ActionOrder.Aim(true);
                targetStrategy.velocity = newVector(currentCenter) - vectorUnit;
                targetStrategy.direction = new Vector(-unit.Direction.Y, unit.Direction.X);

                if (unit.Shield < MyStrategy.constants.MaxShield / 3 && unit.ShieldPotions > 0)
                {
                    targetStrategy.action = new ActionOrder.UseShieldPotion();
                    targetStrategy.velocity = vectorUnit;
                    targetStrategy.direction = vectorUnit;

                    return targetStrategy;
                }

                if (unit.Shield < MyStrategy.constants.MaxShield / 3 && unit.ShieldPotions == 0)
                {
                    Loot targetShieldPotion = GetShieldPotiinNear(unit);



                    Vector vectorTargetShieldPotion = newVector(targetShieldPotion.Position);

                    double distantion = vectorUnit.dist(vectorTargetShieldPotion);

                    if (distantion < 1)
                    {
                        targetStrategy.action = new ActionOrder.Pickup(targetShieldPotion.Id);
                        targetStrategy.velocity = vectorTargetShieldPotion - vectorUnit;
                        targetStrategy.direction = vectorTargetShieldPotion - vectorUnit;
                    }
                    else
                    {
                        targetStrategy.action = new ActionOrder.Aim(true);
                        targetStrategy.velocity = vectorTargetShieldPotion - vectorUnit;
                        targetStrategy.direction = vectorTargetShieldPotion - vectorUnit;
                    }
                }

                if (unit.Weapon != null && unit.Ammo[weapon] < MyStrategy.constants.Weapons[weapon].MaxInventoryAmmo / 4)
                {
                    Loot targetAmmo = GetWeaponAmmoNear(unit);


                    Vector vectortargetAmmo = newVector(targetAmmo.Position);

                    double distantion = vectorUnit.dist(vectortargetAmmo);

                    if (distantion < 1)
                    {
                        targetStrategy.action = new ActionOrder.Pickup(targetAmmo.Id);
                        targetStrategy.velocity = vectortargetAmmo - vectorUnit;
                        targetStrategy.direction = vectortargetAmmo - vectorUnit;
                    }
                    else
                    {
                        targetStrategy.action = new ActionOrder.Aim(true);
                        targetStrategy.velocity = vectortargetAmmo - vectorUnit;
                        targetStrategy.direction = vectortargetAmmo - vectorUnit;
                    }
                }



                return targetStrategy;
            }

            public class strategy
            {
                public Loot loot { get; set; }
                public Unit enemy { get; set; }
                public Unit teammate { get; set; }
                public Projectile bullet { get; set; }
                public ActionOrder action { get; set; }
                public Vector velocity { get; set; }
                public Vector direction { get; set; }

            }

            public class classStrategy : strategy
            {
                //тут можно переопределить методы List или добавить свои
            }


            private static Loot GetShieldPotiinNear(Unit unit)
            {
                Loot? shieldPotion;

                var sp = from Loot u in ShieldPotions orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;

                shieldPotion = sp.FirstOrDefault();


                if (shieldPotion is null)
                {
                    return new Loot();
                }
                else
                {
                    return (Loot)shieldPotion;
                }

            }

            private static Loot GetWeaponAmmoNear(Unit unit)
            {
                Loot? Ammos;

                var am = from Loot u in Ammo orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;

                Ammos = am.FirstOrDefault();


                if (Ammos is null)
                {
                    return new Loot();
                }
                else
                {
                    return (Loot)Ammos;
                }

            }
            public static Vector newVector(Vec2 vec)
            {
                return new Vector(vec.X, vec.Y);
            }
        }
    }

    public class Vector
    {
        public double x, y;


        public Vector()
        {
        }

        public Vector(double x, double y) { this.x = x; this.y = y; }

        public void rotate(double deg)
        {
            double theta = deg / 180.0 * Math.PI;
            double c = Math.Cos(theta);
            double s = Math.Sin(theta);
            double tx = x * c - y * s;
            double ty = x * s + y * c;
            x = tx;
            y = ty;
        }

        public Vector normalize()
        {
            if (length() == 0) return this;
            this.x *= (1.0 / length());
            this.y *= (1.0 / length());
            return this;
        }

        public double dist(Vector v)
        {
            Vector d = new Vector(v.x - x, v.y - y);
            return d.length();
        }

        public double length() { return Math.Sqrt(x * x + y * y); }

        public void truncate(double length)
        {
            double angle = Math.Atan2(y, x);
            x = length * Math.Cos(angle);
            y = length * Math.Sin(angle);
        }

        public Vector ortho()
        {
            return new Vector(y, -x);
        }

        static double dot(Vector v1, Vector v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }
        static double cross(Vector v1, Vector v2)
        {
            return (v1.x * v2.y) - (v1.y * v2.x);
        }

        public static Vector operator +(Vector v1, Vector v2)
        {
            return new Vector(v1.x + v2.x, v1.y + v2.y);
        }
        public static Vector operator -(Vector v1, Vector v2)
        {
            return new Vector(v1.x - v2.x, v1.y - v2.y);
        }

        public static Vector operator *(Vector v, double r)
        {
            return new Vector(v.x * r, v.y * r);
        }

        public static double operator *(Vector v1, Vector v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }

    }

}