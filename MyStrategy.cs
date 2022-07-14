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
        public void DebugUpdate(int displayedTick, DebugInterface debugInterface) { }
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
                        // debugInterface.Add(new DebugData.PlacedText(unit.Position, "en", new Model.Vec2(0, 0), 1.5, new AiCup22.Debugging.Color(0, 0, 1, 1)));
                        //  debugInterface.Add(new DebugData.Ring(unit.Position, 1.5, 0.2, new AiCup22.Debugging.Color(1, 0, 0, 1)));
                        Enemies.Add(unit);
                    }

                }

            }

            internal static UnitOrder GetOrderForTeammate(Unit unit)
            {

                Vector2 newVelocity = GetNewVelocity(unit);
                Vector2 newDirection = GetNewVelocity(unit);// GetNewDirection(unit);
                ActionOrder action = GetNewActionOrder(unit);
                // debugInterface.Add(new DebugData.PolyLine(new Vec2[] { unit.Position, new Vec2(newDirection.x + unit.Position.X, newDirection.y + unit.Position.Y) }, 0.1, new AiCup22.Debugging.Color(1, 0, 1, 1)));
                //  debugInterface.Add(new DebugData.PolyLine(new Vec2[] { unit.Position, new Vec2(newVelocity.x + unit.Position.X, newVelocity.y + unit.Position.Y) }, 0.1, new AiCup22.Debugging.Color(0, 1, 1, 1)));

                //debugInterface.Add(new DebugData.PlacedText(unit.Position, "Here", new Model.Vec2(0, 0), 14.0, new AiCup22.Debugging.Color(1, 0, 1, 1)));
                //debugInterface.Add(new DebugData.PolyLine(new Vec2[] { unit.Position, newVelocity }, "Here", new Model.Vec2(0, 0), 14.0, new AiCup22.Debugging.Color(1, 0, 1, 1)));

                return new UnitOrder(
                                new Vec2(newVelocity.x, newVelocity.y),
                                new Vec2(newDirection.x, newDirection.y),
                                action);

            }

            private static ActionOrder GetNewActionOrder(Unit unit)
            {
                int priority = GetPriorityToMove(unit);

                if (unit.Shield == 0 && unit.ShieldPotions > 0) return new ActionOrder.UseShieldPotion();

                if (priority == 3)
                {
                    return new ActionOrder.Pickup(GetTargetID(unit, priority));
                }
                else
                {
                    return new ActionOrder.Aim(true);
                }
                /* int[] priority = GetPriorityToAim(unit);
                  Cell c = Get(unit.Position);
                  Cell cmin = GetNear(unit.Position, priority, 0);
                  //Cell cmax = GetNearMax(unit.Position);*/
               
                 /* foreach (object o in c.objects)
                  {
                      if (o is Loot) return new ActionOrder.Pickup(((Loot)o).Id);
                  }
                  if (cmin != null) return new ActionOrder.Aim(true);*/
                //return null;*/

               
            }
            private static Vector2 GetNewDirection(Unit unit)
            {
                /*Vector2 newDirection = new Vector2(-unit.Direction.Y, unit.Direction.X);
                Vector2 v = new Vector2(unit.Position.X, unit.Position.Y);
                //Vector2 dir = new Vector2(unit.Direction.X, unit.Direction.Y);
                Vector2 cntr = new Vector2(currentCenter.X, currentCenter.Y);
                double r = v.dist(cntr);
                //if (r > dSize) return newDirection;
                Cell c = Get(unit.Position);
                //if (c.Heat == 0) return newDirection;
                //if(c.objects.Count == 0) return newVelocity;
                int[] priority = GetPriorityToAim(unit);
                Cell cmin = GetNear(unit.Position, priority, 0);
                //Cell cmax = GetNearMax(unit.Position);
                if (cmin == null) return newDirection;
                // if (cmin.objects.Count == 0) return newDirection;
                Unit? enemy = null;
                foreach (object o in cmin.objects)
                    if (o is Model.Unit)
                    {
                        enemy = (Unit)o;
                        break;
                    }

                var p = new Vector2((double)(cmin.x) - dSize, (double)(cmin.y) - dSize);
                if (enemy.HasValue) { p.x = enemy.Value.Position.X; p.y = enemy.Value.Position.Y; }

                //debugInterface.Add(new DebugData.PlacedText(unit.Position, "Here", new Model.Vec2(0, 0), 14.0, new AiCup22.Debugging.Color(1, 0, 1, 1)));


                return (p - v);*/
                return null;

            }
            private static int[] GetPriorityToAim(Unit unit)
            {
                int[] priority = new int[100];

                priority[0]++;

                return priority;
            }
            private static Vector2 GetNewVelocity(Unit unit)
            {

                Vector2 newVelocity = new Vector2(-unit.Position.X, -unit.Position.Y);
                Vector2 v = new Vector2(unit.Position.X, unit.Position.Y);
                Vector2 dir = new Vector2(unit.Direction.X, unit.Direction.Y);
                Vector2 cntr = new Vector2(currentCenter.X, currentCenter.Y);
                double r = v.dist(cntr);
                if (r > dSize) return newVelocity;

                int priority = GetPriorityToMove(unit);

                Vector2 target = GetTarget(unit, priority);

                if (target == null) return newVelocity;

                var p = new Vector2((double)(target.x) - dSize, (double)(target.y) - dSize);

                return (p - v);
            }
            private static int GetPriorityToMove(Unit unit)
            {
                int[] priority = new int[10];

                if (unit.ShieldPotions < MyStrategy.constants.MaxShieldPotionsInInventory) priority[3]++;
                if (unit.Health < MyStrategy.constants.UnitHealth) { priority[3]++; }
                if (unit.Shield > 0 && unit.Weapon != null) priority[0]++;
                if (unit.Weapon == null) { priority[4]++; priority[0]--; }
                int weapon = (int)unit.Weapon;
                if (unit.Weapon != null && unit.Ammo[weapon] < MyStrategy.constants.Weapons[weapon].MaxInventoryAmmo) priority[5]++;

                int target = 0;
                for (int i = 0; i < 6; i++)
                {
                    if (target < priority[i]) target = i;
                }

                return target;

            }

            private static Vector2 GetTarget(Unit unit, int target)
            {
                

                if (target != 3)
                {
                    Unit? enemy;

                    var en = from Unit u in Enemies where u.PlayerId != unit.PlayerId orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;

                    enemy = en.FirstOrDefault();
                    if (enemy is null)
                    {
                        return new Vector2(unit.Position.X, unit.Position.Y);
                    }
                    else
                    {
                        return new Vector2(enemy.Value.Position.X, enemy.Value.Position.Y);
                    }


                }
                else
                {
                    Loot? shieldPotion;
                    
                    var sp = from Loot u in ShieldPotions orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;

                    shieldPotion = sp.FirstOrDefault();
                   
                    if (shieldPotion is null)
                    {
                        return new Vector2(unit.Position.X, unit.Position.Y);
                    }
                    else
                    {
                        return new Vector2(shieldPotion.Value.Position.X, shieldPotion.Value.Position.Y);
                    }


                }
              
                /* if (target == 1)
                 {

                 }
                 if (target == 2)
                 {

                 }
                 if (target == 3)
                 {

                 }
                 if (target == 4)
                 {

                 }
                 if (target == 5)
                 {

                 }

                 mxv += priority[i] * v.heats[i];


                 int x = (int)((position.X + dSize));
                 int y = (int)((position.Y + dSize));


                 Cell c = Get(x, y);

                 int maxv = 0;

                 for (int i = 0; i < 6; i++)
                     maxv += priority[i] * c.heats[i];

                 //if (maxv == 0) return null;



                 bool found = false;
                 foreach (var dir in dirs)
                 {
                     Cell v = Get(dir.x + x, y + dir.y);
                     int mxv = 0;

                     for (int i = 0; i < 6; i++)
                         mxv += priority[i] * v.heats[i];

                     if (mxv == 0) continue;
                     if (mxv > maxv) { c = v; found = true; }
                 }

                 if (found == false) return null;

                 return GetNearRecursive(c.x, c.y, 50, priority, r);
                */
            }
            private static int GetTargetID (Unit unit, int target)
            {


                if (target != 3)
                {
                    Unit? enemy;

                    var en = from Unit u in Enemies where u.PlayerId != unit.PlayerId orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;

                    enemy = en.FirstOrDefault();
                    if (enemy is null)
                    {
                        return 0;
                    }
                    else
                    {
                        return enemy.Value.Id;
                    }


                }
                else
                {
                    Loot? shieldPotion;

                    var sp = from Loot u in ShieldPotions orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;

                    shieldPotion = sp.FirstOrDefault();

                    if (shieldPotion is null)
                    {
                        return 0;
                    }
                    else
                    {
                        return shieldPotion.Value.Id;
                    }


                }

                /* if (target == 1)
                 {

                 }
                 if (target == 2)
                 {

                 }
                 if (target == 3)
                 {

                 }
                 if (target == 4)
                 {

                 }
                 if (target == 5)
                 {

                 }

                 mxv += priority[i] * v.heats[i];


                 int x = (int)((position.X + dSize));
                 int y = (int)((position.Y + dSize));


                 Cell c = Get(x, y);

                 int maxv = 0;

                 for (int i = 0; i < 6; i++)
                     maxv += priority[i] * c.heats[i];

                 //if (maxv == 0) return null;



                 bool found = false;
                 foreach (var dir in dirs)
                 {
                     Cell v = Get(dir.x + x, y + dir.y);
                     int mxv = 0;

                     for (int i = 0; i < 6; i++)
                         mxv += priority[i] * v.heats[i];

                     if (mxv == 0) continue;
                     if (mxv > maxv) { c = v; found = true; }
                 }

                 if (found == false) return null;

                 return GetNearRecursive(c.x, c.y, 50, priority, r);
                */
            }

            private static Vector2 GetNewVeloc1ity(Unit unit)
            {
                Vector2 newVelocity = new Vector2(-unit.Position.X, -unit.Position.Y);
                Vector2 v = new Vector2(unit.Position.X, unit.Position.Y);
                Vector2 dir = new Vector2(unit.Direction.X, unit.Direction.Y);
                Vector2 cntr = new Vector2(currentCenter.X, currentCenter.Y);
                double r = v.dist(cntr);
                if (r > dSize) return newVelocity;

                int priority = GetPriorityToMove(unit);

                Vector2 target = GetTarget(unit, priority);

                if (target == null) return newVelocity;

                // var p = new Vector2((double)(target.x) - dSize, (double)(target.y) - dSize);

                return (target - v);
            }


        }
    }


    public class Vector2
    {
        public double x, y;


        public Vector2()
        {
        }

        public Vector2(double x, double y) { this.x = x; this.y = y; }

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

        public Vector2 normalize()
        {
            if (length() == 0) return this;
            this.x *= (1.0 / length());
            this.y *= (1.0 / length());
            return this;
        }

        public double dist(Vector2 v)
        {
            Vector2 d = new Vector2(v.x - x, v.y - y);
            return d.length();
        }

        public double length() { return Math.Sqrt(x * x + y * y); }

        public void truncate(double length)
        {
            double angle = Math.Atan2(y, x);
            x = length * Math.Cos(angle);
            y = length * Math.Sin(angle);
        }

        public Vector2 ortho()
        {
            return new Vector2(y, -x);
        }

        static double dot(Vector2 v1, Vector2 v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }
        static double cross(Vector2 v1, Vector2 v2)
        {
            return (v1.x * v2.y) - (v1.y * v2.x);
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x + v2.x, v1.y + v2.y);
        }
        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x - v2.x, v1.y - v2.y);
        }

        public static Vector2 operator *(Vector2 v, double r)
        {
            return new Vector2(v.x * r, v.y * r);
        }

        public static double operator *(Vector2 v1, Vector2 v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }

    }

}