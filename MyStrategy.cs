using System.Collections.Generic;
using AiCup22.Model;



namespace AiCup22
{
    public class MyStrategy
    {
        public MyStrategy(AiCup22.Model.Constants constants) {}
        public Order GetOrder(Game game, DebugInterface debugInterface)
        {
            var MyID = 1;
            var EnemyID = 1;
            double XPos = 1.1;
            double YPos = 1.1;
            double XPosEnemy = 1.1;
            double YPosEnemy = 1.1;
            double XPosShieldPotions = 1.1;
            double YPosShieldPotions = 1.1;
            var vrag = 0;
            double rasst = 99999;
            double rasst1 = 99999;
            var lootenable = 0;
            var lootID = 1;
            foreach (var enemy in game.Units)
            {
                if (enemy.PlayerId == game.MyId)
                {
                    MyID = enemy.Id;
                    XPos = enemy.Position.X;
                    YPos = enemy.Position.Y;
                };
            }

            foreach (var loot in game.Loot)
            {
                
                double unit_rasst1 = System.Math.Sqrt(System.Math.Pow(loot.Position.X - XPos, 2) + System.Math.Pow(loot.Position.Y - YPos, 2));
                if (rasst1 > unit_rasst1)
                {
                    rasst1 = unit_rasst1;
                    lootID = loot.Id;
                    XPosShieldPotions = loot.Position.X;
                    YPosShieldPotions = loot.Position.Y;
                    lootenable = 1;
                }



            }

            if (lootenable == 1)
            {

                if (System.Math.Abs(XPosShieldPotions - XPos) > 5)
                {
                    var orders = new Dictionary<int, UnitOrder>()
                    {
                        { MyID, new UnitOrder
                            ( new Vec2((XPosShieldPotions - XPos) , (YPosShieldPotions - YPos)),
                                new Vec2( - YPos,  XPos),
                                new ActionOrder.Aim(true)
                            )
                        }

                    };
                    return new Order(orders);
                }
                else
                {
                    var orders = new Dictionary<int, UnitOrder>()
                    {
                        { MyID, new UnitOrder
                            ( new Vec2((XPosShieldPotions - XPos) , (YPosShieldPotions - YPos)),
                                new Vec2( - XPos,  - YPos),
                                new ActionOrder.Pickup(lootID)
                            )
                        }

                    };
                    return new Order(orders);
                }
        }
            foreach (var enemy in game.Units)
            {
                if (enemy.PlayerId != game.MyId)
                 {
                    double unit_rasst = System.Math.Sqrt(System.Math.Pow(enemy.Position.X - XPos, 2) + System.Math.Pow(enemy.Position.Y - YPos, 2));
                    if (rasst > unit_rasst)
                    {
                        rasst = unit_rasst;
                        EnemyID = enemy.Id;
                        XPosEnemy = enemy.Position.X;
                        YPosEnemy = enemy.Position.Y;
                        vrag = 1;
                    }
 
                };
            };

//
            if (vrag == 1)
            {
                if (lootenable == 1)
                {
                    var orders = new Dictionary<int, UnitOrder>()
                    {
                        { MyID, new UnitOrder
                            ( new Vec2((XPosShieldPotions - XPos) , (YPosShieldPotions - YPos)),
                                new Vec2(XPosEnemy - XPos, YPosEnemy - YPos),
                                new ActionOrder.Aim(true)
                            )
                        }

                    };
                    return new Order(orders);
                }
                else
                {
                    var orders = new Dictionary<int, UnitOrder>()
                    {
                        { MyID, new UnitOrder
                            ( new Vec2((XPosEnemy - XPos) * - 1, (YPosEnemy - YPos) * - 1),
                                new Vec2(XPosEnemy - XPos, YPosEnemy - YPos),
                                new ActionOrder.Aim(true)
                            )
                        }

                    };
                    return new Order(orders);
                }
            }
            else
            {
                if (lootenable == 1)
                {
                    var orders = new Dictionary<int, UnitOrder>()
                    {
                        { MyID, new UnitOrder
                            ( new Vec2((XPosShieldPotions - XPos) , (YPosShieldPotions - YPos)),
                                new Vec2(-YPos, XPos),
                                new ActionOrder.Aim(false)
                            )
                        }

                    };
                    return new AiCup22.Model.Order(orders);
                }
                else
                {
                    var orders = new Dictionary<int, UnitOrder>()
                    {
                        { MyID, new UnitOrder
                            ( new Vec2(-XPos, -YPos),
                                new Vec2(-YPos, XPos),
                                new ActionOrder.Aim(false)
                            )
                        }

                    };
                    return new AiCup22.Model.Order(orders);
                }

               
            }
        }
        public void DebugUpdate(int displayedTick, DebugInterface debugInterface) {}
        public void Finish() {}
    }
}
