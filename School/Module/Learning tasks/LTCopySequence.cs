﻿using GoodAI.Core.Utils;
using GoodAI.Modules.School.Worlds;
using GoodAI.Modules.School.Common;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Drawing;

namespace GoodAI.Modules.School.LearningTasks
{
    public class LTCopySequence : AbstractLearningTask<ManInWorld>
    {
        public const string STOP_REQUEST = "Stop request";

        // True if teacher agent can spawn on different places
        // Teacher should not cover agent
        public const string TEACHER_ON_DIFF_START_POSITION = "Teacher on diff start position";

        protected Random m_rndGen = new Random();
        protected int m_stepsSincePresented = 0;
        protected MovableGameObject m_agent;
        protected AgentsHistory m_agentsHistory;
        protected AbstractTeacherInWorld m_teacher;
        protected AgentsHistory m_teachersHistory;
        protected bool m_delayedCheck = false;

        public LTCopySequence(ManInWorld w)
            : base(w)
        {
            TSHints = new TrainingSetHints
            {
                { STOP_REQUEST, 0},
                { TSHintAttributes.DEGREES_OF_FREEDOM, 1 },
                { TSHintAttributes.NOISE, 0 },
                { TEACHER_ON_DIFF_START_POSITION, 0},
                { TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 10000 }
            };

            TSProgression.Add(TSHints.Clone());
            TSProgression.Add(TSHintAttributes.DEGREES_OF_FREEDOM, 2);
            TSProgression.Add(TSHintAttributes.NOISE, 1);
            TSProgression.Add(TEACHER_ON_DIFF_START_POSITION, 1);
            TSProgression.Add(TSHintAttributes.TARGET_SIZE_STANDARD_DEVIATION, 1.5f);
            TSProgression.Add(STOP_REQUEST, 1);
            TSProgression.Add(TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 1000);
            TSProgression.Add(TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 100);
        }

        protected override void PresentNewTrainingUnit()
        {
            World.ClearWorld();

            CreateAgent();
            CreateTeacher();

            m_stepsSincePresented = 0;
            m_agentsHistory = new AgentsHistory();
            m_agentsHistory.Add(m_agent.X, m_agent.Y);
            m_teachersHistory = new AgentsHistory();
            m_teachersHistory.Add(m_teacher.X, m_teacher.Y);
        }

        protected override bool DidTrainingUnitComplete(ref bool wasUnitSuccessful)
        {
            if (World.IsEmulatingUnitCompletion())
            {
                return World.EmulateIsTrainingUnitCompleted(out wasUnitSuccessful);
            }
            else
            {

                m_stepsSincePresented++;

                if (DidTrainingUnitFail())
                {
                    wasUnitSuccessful = false;
                    Console.WriteLine("FAILED: Time runs out");
                    return true;
                }

                if (!m_teacher.IsDone() && m_agent.isMoving())
                {
                    wasUnitSuccessful = false;
                    Console.WriteLine("FAILED: Moves before teacher ends");
                    return true;
                }

                // save history for agent and teacher
                m_agentsHistory.Add(m_agent.X, m_agent.Y);
                m_teachersHistory.Add(m_teacher.X, m_teacher.Y);

                wasUnitSuccessful = false;

                int numberOfTeachersSteps = m_teachersHistory.numberOfSteps();
                int numberOfAgentsSteps = m_agentsHistory.numberOfSteps();

                // simple version of the task
                if (TSHints[LTCopySequence.STOP_REQUEST] == .0f)
                {
                    if (numberOfTeachersSteps == numberOfAgentsSteps && m_teacher.IsDone())
                    {
                        // compare step
                        wasUnitSuccessful = m_teachersHistory.CompareTo(m_agentsHistory, m_stepsSincePresented);
                        Console.WriteLine("SUCCEED: Easy version");
                        return true;
                    }
                }
                // hard version
                else
                {
                    if (numberOfTeachersSteps == numberOfAgentsSteps && m_teacher.IsDone()) m_delayedCheck = true;

                    if (m_delayedCheck)
                    {
                        m_delayedCheck = false;
                        // compare steps
                        wasUnitSuccessful = m_teachersHistory.CompareTo(m_agentsHistory, m_stepsSincePresented);
                        Console.WriteLine("SUCCEED: Hard version");
                        return true;
                    }
                }
                return false;
            }
        }

        protected bool DidTrainingUnitFail()
        {
            return m_stepsSincePresented > 2 * m_teacher.ActionsCount() + 3;
        }

        protected void CreateAgent()
        {
            m_agent = (World as RoguelikeWorld).CreateAgent();
        }

        protected void CreateTeacher()
        {
            List<RogueTeacher.Actions> actions = new List<RogueTeacher.Actions>();
            actions.Add(RogueTeacher.GetRandomAction(m_rndGen, (int)TSHints[TSHintAttributes.DEGREES_OF_FREEDOM]));
            actions.Add(RogueTeacher.GetRandomAction(m_rndGen, (int)TSHints[TSHintAttributes.DEGREES_OF_FREEDOM]));
            actions.Add(RogueTeacher.GetRandomAction(m_rndGen, (int)TSHints[TSHintAttributes.DEGREES_OF_FREEDOM]));
            actions.Add(RogueTeacher.GetRandomAction(m_rndGen, (int)TSHints[TSHintAttributes.DEGREES_OF_FREEDOM]));
            actions.Add(RogueTeacher.GetRandomAction(m_rndGen, (int)TSHints[TSHintAttributes.DEGREES_OF_FREEDOM]));

            Point teachersPoint;
            if ((int)TSHints[LTCopySequence.TEACHER_ON_DIFF_START_POSITION] != 0)
            {
                teachersPoint = World.GetRandomPositionInsidePow(m_rndGen, RogueTeacher.GetDefaultSize());
            }
            else
            {
                teachersPoint = new Point(m_agent.X + World.POW_WIDTH / 3, m_agent.Y);
            }

            m_teacher = (World as RoguelikeWorld).CreateTeacher(teachersPoint, actions);
        }
    }

    public class AgentsHistory : LinkedList<Point>
    {

        public void Add(Point position)
        {
            this.AddLast(position);
        }

        public void Add(int x, int y)
        {
            this.AddLast(new Point(x, y));
        }

        // compare two histories. Second can be shifted forward
        public bool CompareTo(AgentsHistory h, int numberOfMoves)
        {
            if (this.Count != h.Count)
            {
                throw new ArgumentException();
            }

            LinkedList<Point>.Enumerator h1 = this.GetEnumerator();
            LinkedList<Point>.Enumerator h2 = h.GetEnumerator();

            h1.MoveNext();
            h2.MoveNext();

            Size diff = Size.Subtract(new Size(h2.Current), new Size(h1.Current));

            for (int i = 0; i < numberOfMoves; i++)
            {
                Point norm;
                while (true)
                {
                    norm = h2.Current - diff;
                    if (h1.Current == norm) break;
                    bool moves = h2.MoveNext();
                    if (!moves) return false;
                }
                h1.MoveNext();
                h2.MoveNext();

            }
            return true;
        }

        public bool IsLastDifferent()
        {
            if (this.Count <= 1)
            {
                return false;
            }
            return !this.Last().Equals(this.ElementAt(this.Count() - 2));
        }

        public int numberOfUniquePositions()
        {
            HashSet<Point> h = new HashSet<Point>(this);
            return h.Count;
        }

        public int numberOfSteps()
        {
            int numberOfSteps = 0;
            Enumerator e1 = this.GetEnumerator();
            Enumerator e2 = this.GetEnumerator();
            e2.MoveNext();
            for (int i = 0; i < Count - 1; i++)
            {
                e1.MoveNext();
                e2.MoveNext();
                if (e1.Current != e2.Current) numberOfSteps++;
            }
            return numberOfSteps;
        }
    }
}
