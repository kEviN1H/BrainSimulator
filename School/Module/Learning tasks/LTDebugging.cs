﻿using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.Drawing;
using System.Linq;

namespace GoodAI.Modules.School.LearningTasks
{
    public class LTDebugging : AbstractLearningTask<ManInWorld>
    {
        private GameObject m_target;
        private MovableGameObject m_agent;
        private Random m_rndGen = new Random();
        
        public LTDebugging(ManInWorld w) : base(w)
        {
            TSHints = new TrainingSetHints {
                {TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 10000}
            };

            TSProgression.Add(TSHints.Clone());

            SetHints(TSHints);
        }

        protected override void PresentNewTrainingUnit()
        {
            World.ClearWorld();

            if (World.GetType() == typeof(PlumberWorld)) {
                m_agent = new MovableGameObject(GameObjectType.Agent, @"Plumber24x28.png", 24, 28); ;
                PlumberWorld world = World as PlumberWorld;
                m_target = new GameObject(GameObjectType.NonColliding, @"Coin16x16.png", 200, 200);
                world.AddGameObject(m_target);

                GameObject obj1 = new GameObject(GameObjectType.None, @"Block60x10.png", 10, 260);
                GameObject obj2 = new GameObject(GameObjectType.None, @"Block60x10.png", 100, 250);
                GameObject obj3 = new GameObject(GameObjectType.None, @"Block5x120.png", 200, 100);
                GameObject obj4 = new GameObject(GameObjectType.None, @"Block60x10.png", 300, 200);

                world.AddGameObject(obj1);
                world.AddGameObject(obj2);
                world.AddGameObject(obj3);
                world.AddGameObject(obj4);
            }
            else if (World.GetType() == typeof(RoguelikeWorld))
            {
                
                RoguelikeWorld world = World as RoguelikeWorld;
                world.DegreesOfFreedom = 2;

                // create agent
                m_agent = world.CreateAgent();

                // get grid
                Grid g = world.GetGrid();

                // place objects according to the grid
                m_agent.SetPosition(g.getPoint(15, 16));
                world.CreateWall(g.getPoint(15, 17));
                world.CreateWall(g.getPoint(16, 17));
                world.CreateWall(g.getPoint(17, 17));
                world.CreateWall(g.getPoint(17, 18));

                // place new wall with random position and size (1 - 3 times larger than default)
                GameObject wall = world.CreateWall(g.getPoint(0, 0), (float)(1 + m_rndGen.NextDouble() * 2));
                // GetRandomPositionInsidePow avoids covering agent
                Point randPosition = world.GetRandomPositionInsidePow(m_rndGen, wall.GetGeometry().Size);
                wall.SetPosition(randPosition);

                // create target
                m_target = world.CreateTarget(g.getPoint(18, 18));

                // create shape
                world.CreateShape(
                    g.getPoint(11, 13), // position
                    type: GameObjectType.Obstacle, // type determine interactions
                    color: Color.Cyan, // uses texture shape as mask
                    shape: Shape.Shapes.Star,
                    width: 30, height: 60); // you can resize choosen shape

                RogueDoor door = (RogueDoor) world.CreateDoor(g.getPoint(14, 17));
                world.CreateLever(g.getPoint(13, 13), door);
            }
        }

        protected override bool DidTrainingUnitComplete(ref bool wasUnitSuccessful)
        {
            // check if target was reached
            if (m_agent.ActualCollisions.Contains(m_target)) // Collision check
            {
                GoodAI.Core.Utils.MyLog.INFO.WriteLine("Succes. End of unit!");
                return wasUnitSuccessful = true;
            }
            return wasUnitSuccessful = false;
        }
    
    }

    //public class LTDebuggingRLW : AbstractLearningTask<RoguelikeWorld>
    //{
    //    private GameObject m_target;
    //    private MovableGameObject m_agent;
    //    private Random m_rndGen = new Random();
        
    //    public LTDebuggingRLW(RoguelikeWorld w) : base(w)
    //    {
    //        TSHints = new TrainingSetHints {
    //            {TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 10000}
    //        };

    //        TSProgression.Add(TSHints.Clone());

    //        SetHints(TSHints);
    //    }

    //    protected override void PresentNewTrainingUnit()
    //    {
    //        World.DegreesOfFreedom = 2;
    //        World.ClearWorld();

    //        // create agent
    //        m_agent = World.CreateAgent();

    //        // get grid
    //        Grid g = World.GetGrid();
            
    //        // place objects according to the grid
    //        m_agent.SetPosition(g.getPoint(15, 16));
    //        World.CreateWall(g.getPoint(15, 17)); 
    //        World.CreateWall(g.getPoint(16, 17));
    //        World.CreateWall(g.getPoint(17, 17));
    //        World.CreateWall(g.getPoint(17, 18));

    //        // place new wall with random position and size (1 - 3 times larger than default)
    //        GameObject wall = World.CreateWall(g.getPoint(0,0), (float)(1 + m_rndGen.NextDouble() * 2));
    //        // GetRandomPositionInsidePow avoids covering agent
    //        Point randPosition = World.GetRandomPositionInsidePow(m_rndGen, wall.GetGeometry().Size);
    //        wall.SetPosition(randPosition);

    //        // create target
    //        m_target = World.CreateTarget(g.getPoint(18, 18));

    //        // create shape
    //        World.CreateShape(
    //            g.getPoint(11, 13), // position
    //            type : GameObjectType.Obstacle, // type determine interactions
    //            color : Color.Cyan, // uses texture shape as mask
    //            shape : Shape.Shapes.Star, 
    //            width : 30, height : 60); // you can resize choosen shape

    //        RogueDoor door= World.CreateDoor(g.getPoint(14,17));
    //        World.CreateLever(g.getPoint(13, 13), door);
    //    }

    //    protected override bool DidTrainingUnitComplete(ref bool wasUnitSuccessful)
    //    {
    //        // check if target was reached
    //        if (m_agent.ActualCollisions.Contains(m_target)) // Collision check
    //        {
    //            Console.WriteLine("Succes. End of unit!");
    //            return wasUnitSuccessful = true;
    //        }
    //        return wasUnitSuccessful = false;
    //    }
    //}
}
