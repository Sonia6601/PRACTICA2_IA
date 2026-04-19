using NavigationDJIA.World;
using QMind;
using QMind.Interfaces;
using System;
using UnityEngine;

namespace GrupoA
{
    public class QMindTester : IQMind
    {
        private WorldInfo _worldInfo;
        private QTableStorage _qStorage;
        private QTable _qTable;

        public void Initialize(WorldInfo worldInfo)
        {
            _worldInfo = worldInfo;
            _qStorage = new QTableStorage("TablaQ.csv");
            _qTable = new QTable(_qStorage);
        }

        public CellInfo GetNextStep(CellInfo currentPosition, CellInfo otherPosition)
        {
            // Construimos la clave igual que en el entrenamiento
            string stateKey = BuildStateKey(currentPosition, otherPosition);

            QAction bestAction = _qTable.GetBestAction(stateKey);

            CellInfo nextPosition = ApplyAction(currentPosition, bestAction);

            return nextPosition;
        }

        private string BuildStateKey(CellInfo agent, CellInfo other)
        {
            bool wallUp = IsWall(agent.x, agent.y + 1);
            bool wallDown = IsWall(agent.x, agent.y - 1);
            bool wallLeft = IsWall(agent.x - 1, agent.y);
            bool wallRight = IsWall(agent.x + 1, agent.y);

            // Contar celdas libres en cada direcci n (lookahead de 3 pasos)
            int freeUp = CountFree(agent.x, agent.y, 0, 1, 3);
            int freeDown = CountFree(agent.x, agent.y, 0, -1, 3);
            int freeLeft = CountFree(agent.x, agent.y, -1, 0, 3);
            int freeRight = CountFree(agent.x, agent.y, 1, 0, 3);

            // La direcci n con m s espacio libre
            int escapeDirX = 0, escapeDirY = 0;
            int maxFree = Math.Max(Math.Max(freeUp, freeDown),
                                   Math.Max(freeLeft, freeRight));

            if (maxFree == freeUp) escapeDirY = 1;
            else if (maxFree == freeDown) escapeDirY = -1;
            else if (maxFree == freeRight) escapeDirX = 1;
            else if (maxFree == freeLeft) escapeDirX = -1;

            var state = new QState(agent, other,
                                   wallUp, wallDown, wallLeft, wallRight,
                                   escapeDirX, escapeDirY);
            return state.ToKey();
        }

        private int CountFree(int x, int y, int dx, int dy, int steps)
        {
            int count = 0;
            for (int i = 1; i <= steps; i++)
            {
                int nx = x + dx * i;
                int ny = y + dy * i;
                if (IsWall(nx, ny)) break;
                count++;
            }
            return count;
        }

        private CellInfo ApplyAction(CellInfo agentCell, QAction action)
        {
            switch (action)
            {
                case QAction.Up:
                    return new CellInfo(agentCell.x, agentCell.y + 1);
                case QAction.Down:
                    return new CellInfo(agentCell.x, agentCell.y - 1);
                case QAction.Right:
                    return new CellInfo(agentCell.x + 1, agentCell.y);
                case QAction.Left:
                    return new CellInfo(agentCell.x - 1, agentCell.y);
                default:
                    return new CellInfo(agentCell.x, agentCell.y);
            }
        }

        private bool IsWall(int x, int y)
        {
            if (x < 0 || x >= _worldInfo.WorldSize.x || y < 0 || y >= _worldInfo.WorldSize.y) return true;
            return !_worldInfo[x, y].Walkable;
        }
    }
}