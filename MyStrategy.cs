using System.Collections.Generic;
using AiCup22.Model;
using System;
using System.Linq;
using AiCup22.Debugging;




namespace AiCup22
{
    public class MyStrategy
    {
        public static Constants constants;
        public MyStrategy(Constants constants) { MyStrategy.constants = constants; }
        public Order GetOrder(Game game, DebugInterface debugInterface)
        {
            Arena.debugInterface = debugInterface;
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
            public static List<Projectile> Projectiles;

            public static int size;
            public static double dSize;
            public static int index;
            public static int unitRadius;
            public static Vec2 currentCenter;
            public static double CurrentRadius;
            public static DebugInterface debugInterface;
            public static int tick;



            public static void update(Game game)
            {
                dSize = game.Zone.CurrentRadius;
                unitRadius = (int)MyStrategy.constants.UnitRadius;
                CurrentRadius = game.Zone.CurrentRadius;
                size = (int)CurrentRadius * 2;
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
                Projectiles = new List<Projectile>();


                foreach (var obstacle in MyStrategy.constants.Obstacles)
                    if (!obstacle.CanShootThrough) Obstacles.Add(obstacle);

                foreach (var projectile in game.Projectiles)
                    Projectiles.Add(projectile);

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

                       Enemies.Add(unit);
                    }

                }

            }

            internal static UnitOrder GetOrderForTeammate(Unit unit)
            {

                // var target_case = GetTargetNew(unit);
                var target_case = GetTargetFinal(unit);

                Vector v = new Vector(unit.Position.X, unit.Position.Y);
                Vector cntr = new Vector(currentCenter.X, currentCenter.Y);
                double r = v.dist(cntr) + 5.0 * MyStrategy.constants.UnitRadius;

                if (r > dSize) target_case.velocity = new Vector(-unit.Position.X, -unit.Position.Y);


                foreach (var projectile in Projectiles)
                {
                    Vector pos = newVector(projectile.Position);
                    Vector vel = newVector(projectile.Velocity);

                    Vector upos = newVector(unit.Position);
                    double ur = MyStrategy.constants.UnitRadius;
                    double dist = (upos - pos).length();
                    vel.truncate(dist);
                    Vector npos = pos + (vel);

                    Vector nvel = upos - npos;
                    double inr = nvel.length();
                    nvel.truncate(MyStrategy.constants.MaxUnitForwardSpeed);
                    if (inr <= ur)
                    {
                        target_case.velocity = nvel * 150.0;

                    }
                }
                if (unit.Ammo[(int)unit.Weapon] == 0 && (target_case.action == new Model.ActionOrder.Aim(true) || target_case.action == new Model.ActionOrder.Aim(true)))
                {
                    target_case.action = null;
                }

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





                // если враг есть
                // есть ли оружие/

                if (unit.Shield < MyStrategy.constants.MaxShield / 3 && unit.ShieldPotions > 0)
                {
                    targetStrategy.action = new ActionOrder.UseShieldPotion();
                    targetStrategy.velocity = vectorUnit;
                    targetStrategy.direction = vectorUnit;

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
                    Loot targetAmmo = GetWeaponAmmoNear(unit, 1);


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



                foreach (var projectile in Projectiles)
                {
                    Vector pos = newVector(projectile.Position);
                    Vector vel = newVector(projectile.Velocity);

                    Vector upos = newVector(unit.Position);
                    double ur = MyStrategy.constants.UnitRadius;
                    double dist = (upos - pos).length();
                    vel.truncate(dist);
                    Vector npos = pos + (vel);

                    Vector nvel = pos - npos;
                    double inr = nvel.length();
                    nvel.truncate(MyStrategy.constants.MaxUnitForwardSpeed);

                    if (inr < ur)
                    {
                        targetStrategy.velocity = targetStrategy.velocity + nvel * 15.0;

                    }

                }

                return targetStrategy;
            }

            public static strategy GetTargetFinal(Unit unit)
            {
                string taktikCase = "none";
                Vector myUnit = newVector(unit.Position);
                int? tekWeapon = unit.Weapon;
                if (tekWeapon == null)
                {
                    tekWeapon = 0;
                }
                int AmmoCount = unit.Ammo[(int)tekWeapon];

                Loot EnableShieldPotiinNear = GetShieldPotiinNear(unit);

                Loot EnableWeapon0 = GetWeaponNear(unit, 0);
                Loot EnableWeapon1 = GetWeaponNear(unit, 1);
                Loot EnableWeapon2 = GetWeaponNear(unit, 2);

                Loot EnableAmmoWeapon0 = GetWeaponAmmoNear(unit, 0);
                Loot EnableAmmoWeapon1 = GetWeaponAmmoNear(unit, 1);
                Loot EnableAmmoWeapon2 = GetWeaponAmmoNear(unit, 2);

                Unit EnableEnemynear = GetEnemyNear(unit);

                classStrategy targetStrategy = new classStrategy();

                targetStrategy.velocity = newVector(unit.Velocity);
                targetStrategy.direction = newVector(unit.Direction);

                Vector Centr = newVector(unit.Velocity);

                if (((unit.Health) + (unit.Shield / constants.MaxShield) * 100) / 2 > 50)
                {

                    if (EnableEnemynear.Id != 0)
                    {


                        if (((unit.Health) + (unit.Shield / constants.MaxShield) * 100) / 2 > 75)
                        {


                            if (AmmoCount > 0)
                            {

                                if (unit.Weapon != 2)
                                {

                                    if (EnableWeapon2.Id != 0)
                                    {


                                        if (myUnit.dist(newVector(EnableWeapon2.Position)) < 0.9)
                                        {   // Жизни больше 75%, есть враг в зоне видимости,есть патроны,  оружие не лук, есть лук, он рядом. двигаемся и смотрим на него и подбираем.
                                            taktikCase = "1";
                                            targetStrategy.action = new ActionOrder.Pickup(EnableWeapon2.Id);
                                            targetStrategy.velocity = newVector(EnableWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableWeapon2.Position) - myUnit;
                                        }
                                        else
                                        {
                                            // Жизни больше 75%, есть враг в зоне видимости,есть патроны,  оружие не лук, есть лук. Двигаемся и смотрим на него.
                                            taktikCase = "2";
                                            targetStrategy.action = new ActionOrder.Aim(true);
                                            targetStrategy.velocity = newVector(EnableWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableEnemynear.Position) - myUnit;
                                        }

                                    }
                                    else
                                    {
                                        taktikCase = "678";
                                        targetStrategy.action = null;
                                        targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                        targetStrategy.direction = new Vector(-unit.Direction.Y, unit.Direction.X);
                                    }
                                   
                                }
                                else
                                {
                                    targetStrategy.action = new ActionOrder.Aim(true);
                                    if (myUnit.dist(newVector(EnableEnemynear.Position)) < 10)
                                    {
                                        // Жизни больше 75%, есть враг в зоне видимости,есть патроны, оружие - лук. Двигаемся от врага и смотрим на Врага. 
                                        taktikCase = "3";
                                        targetStrategy.velocity = newVector(EnableEnemynear.Position) + myUnit;
                                    }
                                    else
                                    {
                                        // Жизни больше 75%, есть враг в зоне видимости,есть патроны, оружие - лук. Двигаемся и смотрим на Врага. 
                                        taktikCase = "4";
                                        targetStrategy.velocity = newVector(EnableEnemynear.Position) - myUnit;
                                    }
                                    targetStrategy.direction = newVector(EnableEnemynear.Position) - myUnit;
                                }

                            }
                            else
                            {

                                if (unit.Weapon != 2)
                                {

                                    if (EnableWeapon2.Id != 0)
                                    {

                                        if (myUnit.dist(newVector(EnableWeapon2.Position)) < 0.9)
                                        {
                                            taktikCase = "14";
                                            targetStrategy.action = new ActionOrder.Pickup(EnableWeapon2.Id);
                                            targetStrategy.velocity = newVector(EnableWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableWeapon2.Position) - myUnit;
                                        }
                                        else
                                        {
                                            taktikCase = "15";
                                            targetStrategy.action = null;
                                            targetStrategy.velocity = newVector(EnableWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableWeapon2.Position) - myUnit;
                                        }

                                    }
                                }
                                else
                                {

                                    if (EnableAmmoWeapon2.Id != 0)
                                    {


                                        if (myUnit.dist(newVector(EnableAmmoWeapon2.Position)) < 0.9)
                                        {
                                            taktikCase = "18";
                                            targetStrategy.action = new ActionOrder.Pickup(EnableAmmoWeapon2.Id);
                                            targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        }
                                        else
                                        {
                                            taktikCase = "19";
                                            targetStrategy.action = null;
                                            targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        }

                                    }
                                    else
                                    {
                                        taktikCase = "20";
                                        targetStrategy.action = null;
                                        targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                        targetStrategy.direction.rotate(90);
                                    }
                                }


                            }

                        }
                        else
                        {

                            if (unit.ShieldPotions > 0)
                            {
                                taktikCase = "22";
                                targetStrategy.action = new ActionOrder.UseShieldPotion();
                            }
                            else
                            {

                                if (EnableShieldPotiinNear.Id != 0)
                                {

                                    if (myUnit.dist(newVector(EnableShieldPotiinNear.Position)) < 0.9)
                                    {
                                        taktikCase = "25";
                                        targetStrategy.action = new ActionOrder.Pickup(EnableShieldPotiinNear.Id);
                                        targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                        targetStrategy.direction = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                    }
                                    else
                                    {


                                       // if (EnableEnemynear.Id != 0)
                                       // {
                                            taktikCase = "27";
                                            targetStrategy.action = new ActionOrder.Aim(true);
                                            targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;//to do +
                                            targetStrategy.direction = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                       // }
                                       // else
                                       // {
                                        //    taktikCase = "28";
                                        //    targetStrategy.action = null;
                                       //     targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                        //}

                                    }
                                }
                                else
                                {

                                    if (EnableEnemynear.Id != 0)
                                    {
                                        taktikCase = "30";
                                        targetStrategy.action = null;
                                        targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                        targetStrategy.direction = new Vector(-unit.Direction.Y, unit.Direction.X);
                                    }
                                    else
                                    {

                                        targetStrategy.action = new ActionOrder.UseShieldPotion();
                                        if (myUnit.dist(newVector(EnableEnemynear.Position)) < 10)
                                        {
                                            taktikCase = "31";
                                            targetStrategy.velocity = newVector(EnableEnemynear.Position) + myUnit;
                                        }
                                        else
                                        {
                                            taktikCase = "32";
                                            targetStrategy.velocity = newVector(EnableEnemynear.Position) - myUnit;
                                        }

                                        targetStrategy.direction.rotate(90);
                                    }

                                }
                            }
                        }
                    }
                    else
                    {

                        if (unit.ShieldPotions > 3)
                        {

                            if (AmmoCount > constants.Weapons[(int)tekWeapon].MaxInventoryAmmo * 0.7)
                            {
                               
                                if (tekWeapon == 2)
                                {
                                    taktikCase = "35";
                                    targetStrategy.action = null;
                                    targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                    targetStrategy.direction.rotate(90);
                                }
                                else
                                {

                                    if (EnableAmmoWeapon2.Id != 0)
                                    {

                                        if (myUnit.dist(newVector(EnableAmmoWeapon2.Position)) < 0.9)
                                        {
                                            taktikCase = "37";
                                            targetStrategy.action = new ActionOrder.Pickup(EnableAmmoWeapon2.Id);
                                            targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        }
                                        else
                                        {
                                            taktikCase = "38";
                                            targetStrategy.action = null;
                                            targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        }

                                    }
                                    else
                                    {
                                        taktikCase = "39";
                                        targetStrategy.action = null;
                                        targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                        targetStrategy.direction.rotate(90);
                                    }

                                }
                            }
                            else
                            {

                                if (EnableAmmoWeapon2.Id != 0)
                                {

                                    if (myUnit.dist(newVector(EnableAmmoWeapon2.Position)) < 0.9)
                                    {
                                        taktikCase = "41";
                                        targetStrategy.action = new ActionOrder.Pickup(EnableAmmoWeapon2.Id);
                                        targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                    }
                                    else
                                    {
                                        taktikCase = "42";
                                        targetStrategy.action = null;
                                        targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                    }

                                }
                                else
                                {

                                    if (EnableAmmoWeapon2.Id != 0)
                                    {

                                        if (myUnit.dist(newVector(EnableAmmoWeapon2.Position)) < 0.9)
                                        {
                                            taktikCase = "43";
                                            targetStrategy.action = new ActionOrder.Pickup(EnableAmmoWeapon2.Id);
                                            targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        }
                                        else
                                        {
                                            taktikCase = "44";
                                            targetStrategy.action = null;
                                            targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        }

                                    }
                                    else
                                    {

                                        if (EnableAmmoWeapon0.Id != 0)
                                        {

                                            if (myUnit.dist(newVector(EnableAmmoWeapon0.Position)) < 0.9)
                                            {
                                                taktikCase = "45";
                                                targetStrategy.action = new ActionOrder.Pickup(EnableAmmoWeapon0.Id);
                                                targetStrategy.velocity = newVector(EnableAmmoWeapon0.Position) - myUnit;
                                                targetStrategy.direction = newVector(EnableAmmoWeapon0.Position) - myUnit;
                                            }
                                            else
                                            {
                                                taktikCase = "46";
                                                targetStrategy.action = null;
                                                targetStrategy.velocity = newVector(EnableAmmoWeapon0.Position) - myUnit;
                                                targetStrategy.direction = newVector(EnableAmmoWeapon0.Position) - myUnit;
                                            }

                                        }
                                        else
                                        {
                                            taktikCase = "47";
                                            targetStrategy.action = null;
                                            targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                            targetStrategy.direction.rotate(90);
                                        }
                                    }


                                }

                            }
                        }
                        else
                        {

                            if (EnableShieldPotiinNear.Id != 0)
                            {

                                if (myUnit.dist(newVector(EnableShieldPotiinNear.Position)) < 0.9)
                                {
                                    taktikCase = "48";
                                    targetStrategy.action = new ActionOrder.Pickup(EnableShieldPotiinNear.Id);
                                    targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                    targetStrategy.direction = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                }
                                else
                                {

                                    // Жизни больше 50%, врагов нет,зелья не на максимуме,зелья есть в зоне видимости, но не рядом. Двигаемся и смотрим на Врага.
                                    taktikCase = "50";
                                    targetStrategy.action = null;
                                    targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;

                                }
                            }
                            else
                            {

                                if (EnableEnemynear.Id != 0)
                                {
                                    taktikCase = "51";
                                    targetStrategy.action = null;
                                    targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                    targetStrategy.direction = new Vector(-unit.Direction.Y, unit.Direction.X);
                                }
                                else
                                {

                                    targetStrategy.action = new ActionOrder.UseShieldPotion();
                                    if (myUnit.dist(newVector(EnableEnemynear.Position)) < 10)
                                    {
                                        taktikCase = "52";
                                        targetStrategy.velocity = newVector(EnableEnemynear.Position) + myUnit;
                                    }
                                    else
                                    {
                                        taktikCase = "53";
                                        targetStrategy.velocity = newVector(EnableEnemynear.Position) - myUnit;
                                    }
                                    targetStrategy.direction.rotate(90);
                                }

                            }
                        }
                    }
                }
                else
                {

                    if (unit.ShieldPotions > 0)
                    {
                        taktikCase = "54";
                        targetStrategy.action = new ActionOrder.UseShieldPotion();
                    }
                    else
                    {

                        if (EnableShieldPotiinNear.Id != 0)
                        {

                            if (myUnit.dist(newVector(EnableShieldPotiinNear.Position)) < 0.9)
                            {
                                taktikCase = "55";
                                targetStrategy.action = new ActionOrder.Pickup(EnableShieldPotiinNear.Id);
                                targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                targetStrategy.direction = newVector(EnableShieldPotiinNear.Position) - myUnit;
                            }
                            else
                            {

                                if (EnableEnemynear.Id != 0)
                                {
                                    taktikCase = "56";
                                    targetStrategy.action = new ActionOrder.Aim(true);
                                    targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                    targetStrategy.direction = newVector(EnableEnemynear.Position) - myUnit;
                                }
                                else
                                {
                                    taktikCase = "57";
                                    targetStrategy.action = null;
                                    targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                }

                            }
                        }
                        else
                        {

                            if (EnableEnemynear.Id != 0)
                            {
                                taktikCase = "58";
                                targetStrategy.action = null;
                                targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                targetStrategy.direction = new Vector(-unit.Direction.Y, unit.Direction.X);
                            }
                            else
                            {

                                targetStrategy.action = new ActionOrder.UseShieldPotion();
                                if (myUnit.dist(newVector(EnableEnemynear.Position)) < 10)
                                {
                                    taktikCase = "59";
                                    targetStrategy.velocity = newVector(EnableEnemynear.Position) + myUnit;
                                }
                                else
                                {
                                    taktikCase = "60";
                                    targetStrategy.velocity = newVector(EnableEnemynear.Position) - myUnit;
                                }
                                targetStrategy.direction.rotate(90);
                            }

                        }
                    }
                }

                string taktikCasetext = "";
                if (taktikCase == "1")
                    taktikCasetext = "Жизни больше 75%, есть враг в зоне видимости,есть патроны,  оружие не лук, есть лук, он рядом. двигаемся и смотрим на него и подбираем.1 ";

                if (taktikCase == "2")
                    taktikCasetext = "Жизни больше 75%, есть враг в зоне видимости,есть патроны,  оружие не лук, есть лук. Двигаемся и смотрим на него. 2";

                if (taktikCase == "3")
                    taktikCasetext = " // Жизни больше 75%, есть враг в зоне видимости,есть патроны, оружие - лук. Двигаемся от врага и смотрим на Врага. 3";

                if (taktikCase == "4")
                    taktikCasetext = "Жизни больше 75%, есть враг в зоне видимости,есть патроны, оружие - лук. Двигаемся и смотрим на Врага. 4";

                if (taktikCase == "50")
                    taktikCasetext = "Жизни больше 50 %, врагов нет, зелья не на максимуме,зелья есть в зоне видимости, но не рядом.Двигаемся и смотрим на Врага. 50 ";
                
                if (taktikCase == "678")
                    taktikCasetext = "Кружимся к центру. 678";




                if (taktikCasetext == "")
                    taktikCasetext = taktikCase;
                //debugInterface.Add(new DebugData.PlacedText(unit.Position, taktikCasetext, new Vec2(1, 1), 1, new Debugging.Color(0, 0, 0, 100)));

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

            public static void logg(int num, Vector myUnit, int? tekWeapon, int AmmoCount, Loot EnableShieldPotiinNear, Loot EnableWeapon0,
                 Loot EnableWeapon1, Loot EnableWeapon2, Unit EnableEnemynear, classStrategy targetStrategy)
            {

                /* Console.WriteLine($"{tick}: myUnit :" + $"{myUnit} " + "num case:" + $"{num} " + "tekWeapon :" + $"{myUnit} " + "tekWeapon :" + $"{tekWeapon}"
                     + "AmmoCount :" + $"{AmmoCount}" + "EnableShieldPotiinNear :" + $"{EnableShieldPotiinNear}" + "EnableWeapon0 :" + $"{EnableWeapon0}"
                     + "EnableWeapon1 :" + $"{EnableWeapon1}" + "EnableWeapon2 :" + $"{EnableWeapon2}" + "EnableEnemynear :" + $"{EnableEnemynear}");
             */
            }
            public class classStrategy : strategy
            {
                //тут можно переопределить методы List или добавить свои
            }


            private static Loot GetShieldPotiinNear(Unit unit)
            {
              
                var sp = from Loot u in ShieldPotions orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;


                foreach(Loot shieldPotion in sp)
                {
                    if ((newVector(shieldPotion.Position) - newVector(currentCenter)).length() < CurrentRadius)
                    {
                        return (Loot)shieldPotion;
                    }
                }

                return new Loot();
        
            }


            private static Loot GetWeaponNear(Unit unit, int typeIndex)
            {

                var wp = from Loot u in Weapon where ((AiCup22.Model.Item.Weapon)u.Item).TypeIndex == typeIndex orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;

                foreach (Loot weapon in wp)
                {
                    if ((newVector(weapon.Position) - newVector(currentCenter)).length() < CurrentRadius)
                    {
                        return (Loot)weapon;
                    }
                }

                return new Loot();

            }

            private static Unit GetEnemyNear(Unit unit)
            {
                Unit? Enemy;

                var en = from Unit u in Enemies orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;

                Enemy = en.FirstOrDefault();


                if (Enemy is null)
                {
                    return new Unit();
                }
                else
                {
                    return (Unit)Enemy;
                }

            }

            private static Loot GetWeaponAmmoNear(Unit unit, int typeIndex)
            {       
                var am = from Loot u in Ammo where ((AiCup22.Model.Item.Ammo)u.Item).WeaponTypeIndex == typeIndex orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;

                foreach (Loot Ammos in am)
                {
                    if ((newVector(Ammos.Position) - newVector(currentCenter)).length() < CurrentRadius)
                    {
                        return (Loot)Ammos;
                    }
                }

                return new Loot();

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
        public override string ToString()
        {

            return $"[{this.x},{this.y}]";
        }
    }

}
