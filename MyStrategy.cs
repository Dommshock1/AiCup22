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
            public static List<Projectile> Projectiles;

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
                Projectiles = new List<Projectile>();
                Weapon = new List<Loot>();
                Ammo = new List<Loot>();

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

                        // debugInterface.AddPlacedText(unit.Position, "enвывфывфыв", new Model.Vec2(0, 0), 1.5, new AiCup22.Debugging.Color(0, 0, 1, 1));
                        //debugInterface.Add(new  Debugging.DebugData.PlacedText(unit.Position, "enвывфывфыв", new Model.Vec2(0, 0), 1.5, new AiCup22.Debugging.Color(0, 0, 1, 1)));
                        //debugInterface.Add(new Debugging.DebugData.Ring(unit.Position, 1.5, 0.2, new AiCup22.Debugging.Color(1, 0, 0, 1)));
                        Enemies.Add(unit);
                    }

                }

            }

            internal static UnitOrder GetOrderForTeammate(Unit unit)
            {

                // var target_case = GetTargetNew(unit);
                var target_case = GetTargetFinal(unit);

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
                    logg(1, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);

                    if (EnableEnemynear.Id != 0)
                    {
                        logg(2, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                        if (((unit.Health) + (unit.Shield / constants.MaxShield) * 100) / 2 > 100)
                        {
                            logg(3, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);

                            if (AmmoCount > 0)
                            {
                                logg(4, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                targetStrategy.action = new ActionOrder.Aim(true);
                                targetStrategy.velocity = newVector(EnableEnemynear.Position) - myUnit;
                                targetStrategy.direction = newVector(EnableEnemynear.Position) - myUnit;
                            }
                            else
                            {
                                logg(5, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                if (EnableAmmoWeapon2.Id != 0)
                                {
                                    logg(6, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    if (myUnit.dist(newVector(EnableAmmoWeapon2.Position)) < 2)
                                    {
                                        logg(7, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        targetStrategy.action = new ActionOrder.Pickup(EnableAmmoWeapon2.Id);
                                        targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                    }
                                    else
                                    {
                                        logg(8, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        targetStrategy.action = new ActionOrder.Aim(false);
                                        targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                    }

                                }
                                else
                                {
                                    logg(9, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    if (EnableAmmoWeapon1.Id != 0)
                                    {
                                        logg(10, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        if (myUnit.dist(newVector(EnableAmmoWeapon2.Position)) < 2)
                                        {
                                            logg(1, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                            targetStrategy.action = new ActionOrder.Pickup(EnableAmmoWeapon1.Id);
                                            targetStrategy.velocity = newVector(EnableAmmoWeapon1.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableAmmoWeapon1.Position) - myUnit;
                                        }
                                        else
                                        {
                                            logg(12, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                            targetStrategy.action = new ActionOrder.Aim(false);
                                            targetStrategy.velocity = newVector(EnableAmmoWeapon1.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableAmmoWeapon1.Position) - myUnit;
                                        }

                                    }
                                    else
                                    {
                                        logg(13, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        if (EnableAmmoWeapon0.Id != 0)
                                        {
                                            logg(14, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                            if (myUnit.dist(newVector(EnableAmmoWeapon0.Position)) < 2)
                                            {
                                                logg(15, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                                targetStrategy.action = new ActionOrder.Pickup(EnableAmmoWeapon0.Id);
                                                targetStrategy.velocity = newVector(EnableAmmoWeapon0.Position) - myUnit;
                                                targetStrategy.direction = newVector(EnableAmmoWeapon0.Position) - myUnit;
                                            }
                                            else
                                            {
                                                logg(16, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                                targetStrategy.action = new ActionOrder.Aim(false);
                                                targetStrategy.velocity = newVector(EnableAmmoWeapon0.Position) - myUnit;
                                                targetStrategy.direction = newVector(EnableAmmoWeapon0.Position) - myUnit;
                                            }

                                        }
                                        else
                                        {
                                            logg(17, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                            targetStrategy.action = new ActionOrder.Aim(false);
                                            targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                            targetStrategy.direction.rotate(180);
                                        }
                                    }


                                }
                            }

                        }
                        else
                        {
                            logg(18, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                            if (unit.ShieldPotions > 0)
                            {
                                logg(19, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                targetStrategy.action = new ActionOrder.UseShieldPotion();
                            }
                            else
                            {
                                logg(20, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                if (EnableShieldPotiinNear.Id != 0)
                                {
                                    logg(21, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    if (myUnit.dist(newVector(EnableShieldPotiinNear.Position)) < 2)
                                    {
                                        logg(2, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        targetStrategy.action = new ActionOrder.Pickup(EnableShieldPotiinNear.Id);
                                        targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                        targetStrategy.direction = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                    }
                                    else
                                    {
                                        logg(23, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        if (EnableEnemynear.Id != 0)
                                        {
                                            logg(24, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                            targetStrategy.action = new ActionOrder.Aim(true);
                                            targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableEnemynear.Position) - myUnit;
                                        }
                                        else
                                        {
                                            logg(25, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                            targetStrategy.action = new ActionOrder.Aim(false);
                                            targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                        }

                                    }
                                }
                                else
                                {
                                    logg(26, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    if (EnableEnemynear.Id != 0)
                                    {
                                        logg(27, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        targetStrategy.action = new ActionOrder.Aim(false);
                                        targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                        targetStrategy.direction = new Vector(-unit.Direction.Y, unit.Direction.X);
                                    }
                                    else
                                    {
                                        logg(28, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        targetStrategy.action = new ActionOrder.UseShieldPotion();
                                        targetStrategy.velocity = newVector(EnableEnemynear.Position) - myUnit;
                                        targetStrategy.direction.rotate(180);
                                    }

                                }
                            }
                        }
                    }
                    else
                    {
                        logg(29, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                        if (unit.ShieldPotions == MyStrategy.constants.MaxShieldPotionsInInventory)
                        {
                            logg(30, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                            if (AmmoCount > 50)
                            {
                                logg(31, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                if (tekWeapon == 2)
                                {
                                    logg(32, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    targetStrategy.action = new ActionOrder.Aim(false);
                                    targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                    targetStrategy.direction.rotate(180);
                                }
                                else
                                {
                                    logg(33, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    if (EnableAmmoWeapon2.Id != 0)
                                    {
                                        logg(34, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        if (myUnit.dist(newVector(EnableAmmoWeapon2.Position)) < 2)
                                        {
                                            logg(35, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                            targetStrategy.action = new ActionOrder.Pickup(EnableAmmoWeapon2.Id);
                                            targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        }
                                        else
                                        {
                                            logg(36, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                            targetStrategy.action = new ActionOrder.Aim(false);
                                            targetStrategy.velocity = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                            targetStrategy.direction = newVector(EnableAmmoWeapon2.Position) - myUnit;
                                        }

                                    }
                                    else
                                    {
                                        logg(37, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        targetStrategy.action = new ActionOrder.Aim(false);
                                        targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                        targetStrategy.direction.rotate(180);
                                    }

                                }
                            }
                            else
                            {
                                logg(38, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);

                            }
                        }
                        else
                        {
                            logg(39, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                            if (EnableShieldPotiinNear.Id != 0)
                            {
                                logg(40, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                if (myUnit.dist(newVector(EnableShieldPotiinNear.Position)) < 2)
                                {
                                    logg(41, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    targetStrategy.action = new ActionOrder.Pickup(EnableShieldPotiinNear.Id);
                                    targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                    targetStrategy.direction = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                }
                                else
                                {
                                    logg(42, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    if (EnableEnemynear.Id != 0)
                                    {
                                        logg(43, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        targetStrategy.action = new ActionOrder.Aim(true);
                                        targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                        targetStrategy.direction = newVector(EnableEnemynear.Position) - myUnit;
                                    }
                                    else
                                    {
                                        logg(44, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                        targetStrategy.action = new ActionOrder.Aim(false);
                                        targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                    }

                                }
                            }
                            else
                            {
                                logg(45, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                if (EnableEnemynear.Id != 0)
                                {
                                    logg(46, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    targetStrategy.action = new ActionOrder.Aim(false);
                                    targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                    targetStrategy.direction = new Vector(-unit.Direction.Y, unit.Direction.X);
                                }
                                else
                                {
                                    logg(47, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    targetStrategy.action = new ActionOrder.UseShieldPotion();
                                    targetStrategy.velocity = newVector(EnableEnemynear.Position) - myUnit;
                                    targetStrategy.direction.rotate(180);
                                }

                            }
                        }
                    }
                }
                else
                {
                    logg(48, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                    if (unit.ShieldPotions > 0)
                    {
                        logg(49, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                        targetStrategy.action = new ActionOrder.UseShieldPotion();
                    }
                    else
                    {
                        logg(50, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                        if (EnableShieldPotiinNear.Id != 0)
                        {
                            logg(51, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                            if (myUnit.dist(newVector(EnableShieldPotiinNear.Position)) < 2)
                            {
                                logg(52, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                targetStrategy.action = new ActionOrder.Pickup(EnableShieldPotiinNear.Id);
                                targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                targetStrategy.direction = newVector(EnableShieldPotiinNear.Position) - myUnit;
                            }
                            else
                            {
                                logg(53, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                if (EnableEnemynear.Id != 0)
                                {
                                    logg(54, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    targetStrategy.action = new ActionOrder.Aim(true);
                                    targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                    targetStrategy.direction = newVector(EnableEnemynear.Position) - myUnit;
                                }
                                else
                                {
                                    logg(55, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                    targetStrategy.action = new ActionOrder.Aim(false);
                                    targetStrategy.velocity = newVector(EnableShieldPotiinNear.Position) - myUnit;
                                }

                            }
                        }
                        else
                        {
                            logg(56, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                            if (EnableEnemynear.Id != 0)
                            {
                                logg(57, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                targetStrategy.action = new ActionOrder.Aim(false);
                                targetStrategy.velocity = newVector(currentCenter) - myUnit;
                                targetStrategy.direction = new Vector(-unit.Direction.Y, unit.Direction.X);
                            }
                            else
                            {
                                logg(58, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                   EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                                targetStrategy.action = new ActionOrder.UseShieldPotion();
                                targetStrategy.velocity = newVector(EnableEnemynear.Position) - myUnit;
                                targetStrategy.direction.rotate(180);
                            }

                        }
                    }
                }

                logg(777, myUnit, tekWeapon, AmmoCount, EnableShieldPotiinNear, EnableWeapon0,
                  EnableWeapon1, EnableWeapon2, EnableEnemynear, targetStrategy);
                Console.WriteLine($"{tick}: itog=");
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

                Console.WriteLine($"{tick}: myUnit :" + $"{ myUnit} " + "num case:" + $"{ num} " + "tekWeapon :" + $"{ myUnit} " + "tekWeapon :" + $"{ tekWeapon}"
                    + "AmmoCount :" + $"{ AmmoCount}" + "EnableShieldPotiinNear :" + $"{ EnableShieldPotiinNear}" + "EnableWeapon0 :" + $"{ EnableWeapon0}"
                    + "EnableWeapon1 :" + $"{ EnableWeapon1}" + "EnableWeapon2 :" + $"{ EnableWeapon2}" + "EnableEnemynear :" + $"{ EnableEnemynear}");
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


            private static Loot GetWeaponNear(Unit unit, int typeIndex)
            {
                Loot? weapon;

                var wp = from Loot u in Weapon where ((AiCup22.Model.Item.Weapon)u.Item).TypeIndex == typeIndex orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;

                weapon = wp.FirstOrDefault();


                if (weapon is null)
                {
                    return new Loot();
                }
                else
                {
                    return (Loot)weapon;
                }

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
                Loot? Ammos;

                var am = from Loot u in Ammo where ((AiCup22.Model.Item.Ammo)u.Item).WeaponTypeIndex == typeIndex orderby (Math.Pow(u.Position.X - unit.Position.X, 2.0) + Math.Pow(u.Position.Y - unit.Position.Y, 2)) select u;

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
        public override string ToString()
        {

            return $"[{this.x},{this.y}]";
        }
    }

}