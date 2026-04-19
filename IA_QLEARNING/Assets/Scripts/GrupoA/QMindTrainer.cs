using NavigationDJIA.Interfaces;
using NavigationDJIA.World;
using QMind;
using QMind.Interfaces;
using System;
using Unity.VisualScripting;
using UnityEngine;

namespace GrupoA
{
    public class QMindTrainer : IQMindTrainer
    {
        private QMindTrainerParams _params;
        private WorldInfo _worldInfo;
        INavigationAlgorithm _navigationAlgorithm;

        private QTableStorage _qStorage;
        private QTable _qTable;

        private CellInfo _agentPosition;
        private CellInfo _otherPosition;

        private float _return;
        private float _returnAveraged;
        private System.Random _random = new System.Random();

        // A partir de aqui has el // son variables que no tiene el otro codigo
        private float currentEpsilon;
        private float initialEpsilon = 0.9f;
        private float minEpsilon = 0.1f;
        private float epsilonDecay = 0.9995f; // Decae lentamente
        private float _lastLoggedEpsilon = -1f;


        private int _bestEpisode = 0;
        private int _bestSteps = 0;

        private int _totalSteps = 0;
        private int _finishedEpisodes = 0;

        //

        #region IQMindTrainer implementation

        public CellInfo AgentPosition => _agentPosition;
        public CellInfo OtherPosition => _otherPosition;

        public int CurrentEpisode { get; private set; }
        public int CurrentStep { get; private set; }

        public float Return => _return;
        public float ReturnAveraged => _returnAveraged;

        public event EventHandler OnEpisodeStarted;
        public event EventHandler OnEpisodeFinished;

        #endregion

        public void Initialize(QMindTrainerParams qMindTrainerParams, WorldInfo worldInfo, INavigationAlgorithm navigationAlgorithm)
        {
            _params = qMindTrainerParams;
            _worldInfo = worldInfo;
            _navigationAlgorithm = navigationAlgorithm;
            _navigationAlgorithm.Initialize(worldInfo);

            _qStorage = new QTableStorage("TablaQ.csv");
            _qTable = new QTable(_qStorage);

            currentEpsilon = _params.epsilon;
            CurrentEpisode = 0;
            StartNewEpisode();
        }

        private void StartNewEpisode()
        {

            CurrentEpisode++;
            CurrentStep = 0;
            _return = 0f;
            _returnAveraged = 0f;

            _agentPosition = _worldInfo.RandomCell();
            _otherPosition = _worldInfo.RandomCell();

            //if (ManhattanDistance(_agentPosition, _otherPosition) < 8)
            //    _otherPosition = _worldInfo.RandomCell();

            OnEpisodeStarted?.Invoke(this, EventArgs.Empty);
        }

        private void EndEpisode()
        {
            //DecayEpsilon();

            #region Debuging
            // Acumulamos pasos para la media
            _totalSteps += CurrentStep;
            _finishedEpisodes++;

            //Para comprobar la mejor iteracion
            if (CurrentStep > _bestSteps)
            {
                _bestSteps = CurrentStep;
                _bestEpisode = CurrentEpisode;
            }
            #endregion

            _qTable.SaveToCsv();

            Debug.Log($"[END EPISODE] {CurrentEpisode} | Steps: {CurrentStep} | Epsilon: {currentEpsilon:F4}");

            OnEpisodeFinished?.Invoke(this, EventArgs.Empty);

            if (_params.episodes > 0 && CurrentEpisode >= _params.episodes)
            {
                float averageSteps = (float)_totalSteps / _finishedEpisodes;

                Debug.Log(
                $"[Q-Learning FINALIZADO]\n" +
                $"Episodios totales: {_finishedEpisodes}\n" +
                $"Mejor episodio: {_bestEpisode} con {_bestSteps} pasos\n" +
                $"Media de pasos por episodio: {averageSteps:F2}"
                );
                return;
            }

            StartNewEpisode();
        }

        public void DoStep(bool train)
        {
            // Estado actual del agente
            string stateKey = BuildStateKey(_agentPosition, _otherPosition);

            // Seleciona la acción a realizar
            QAction action = ChooseAction(stateKey, train);

            // Nuevos estados del agente y del oponente
            CellInfo newAgentPos = ApplyAction(_agentPosition, action);
            CellInfo newOtherPos = MoveOpponent(_otherPosition, newAgentPos.Walkable ? newAgentPos : _agentPosition);

            // Nuevo estado del agente
            string nextStateKey = BuildStateKey(newAgentPos, newOtherPos);

            // Calcula la recompensa
            float reward = ComputeReward(_agentPosition, _otherPosition, newAgentPos, newOtherPos, action);

            if (train)
            {
                UpdateQ(stateKey, action, reward, nextStateKey);
            }

            // actualiza las posiciones
            _agentPosition = newAgentPos;
            _otherPosition = newOtherPos;

            // Actualizamos estadísticas de recompensas
            CurrentStep++;
            _return += reward;
            _returnAveraged = (_returnAveraged * (CurrentStep - 1) + reward) / CurrentStep;

            // Comprobación de si estamos en el fin de episodio
            if (IsTerminalState(_agentPosition, _otherPosition))
            {

                EndEpisode();
            }
        }

        #region Parte a implementar por el alumno

        //private void DecayEpsilon()
        //{
        //    float oldEpsilon = currentEpsilon;
        //    // Reducimos epsilon multiplicándolo por el factor de decaimiento,
        //    // pero nunca bajamos del mínimo establecido.
        //    currentEpsilon = Mathf.Max(minEpsilon, currentEpsilon * epsilonDecay);

        //    // Log opcional para no saturar la consola
        //    if (Mathf.Abs(currentEpsilon - _lastLoggedEpsilon) > 0.05f)
        //    {
        //        // Debug.Log($"[EPSILON] {currentEpsilon:F4}");
        //        _lastLoggedEpsilon = currentEpsilon;
        //    }
        //}

        private string BuildStateKey(CellInfo agent, CellInfo other)
        {
            bool wallUp = IsWall(agent.x, agent.y + 1);
            bool wallDown = IsWall(agent.x, agent.y - 1);
            bool wallLeft = IsWall(agent.x - 1, agent.y);
            bool wallRight = IsWall(agent.x + 1, agent.y);

            // Contar celdas libres en cada dirección (lookahead de 3 pasos)
            int freeUp = CountFree(agent.x, agent.y, 0, 1, 3);
            int freeDown = CountFree(agent.x, agent.y, 0, -1, 3);
            int freeLeft = CountFree(agent.x, agent.y, -1, 0, 3);
            int freeRight = CountFree(agent.x, agent.y, 1, 0, 3);

            // La dirección con más espacio libre
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

        private bool IsWall(int x, int y)
        {
            if (x < 0 || x >= _worldInfo.WorldSize.x || y < 0 || y >= _worldInfo.WorldSize.y) return true;
            return !_worldInfo[x, y].Walkable;
        }

        private QAction ChooseAction(string stateKey, bool train)
        {
            if (!train) return _qTable.GetBestAction(stateKey);

            // Si epsilon ya es muy bajo, asegúrate de que no sea 0 del todo 
            // para que siempre haya una mínima posibilidad de descubrir algo nuevo.

            if (_random.NextDouble() < _params.epsilon)
            {
                return (QAction)_random.Next(Enum.GetValues(typeof(QAction)).Length);
            }
            return _qTable.GetBestAction(stateKey);
        }

        private void UpdateQ(string stateKey, QAction action, float reward, string nextStateKey)
        {
            float oldQ = _qTable.GetQ(stateKey, action);
            float maxQNext = _qTable.GetMaxQ(nextStateKey);

            // Con Gamma 0.9, el agente valora mucho el futuro.
            // Esta fórmula actualizará los 92 estados muy rápido.
            float target = reward + _params.gamma * maxQNext;
            float newQ = (1 - _params.alpha) * oldQ + _params.alpha * target;

            _qTable.SetQ(stateKey, action, newQ);
        }

        #endregion

        private float ComputeReward(CellInfo oldAgentPos, CellInfo oldEnemyPos,
                             CellInfo newAgentPos, CellInfo newEnemyPos, QAction action)
        {
            if (newAgentPos == newEnemyPos || newAgentPos == null || !newAgentPos.Walkable)
                return -5000f;

            // Penalización por no moverse (choque con muro)
            if (newAgentPos == oldAgentPos)
                return -200f;

            int oldDist = ManhattanDistance(oldAgentPos, oldEnemyPos);
            int newDist = ManhattanDistance(newAgentPos, newEnemyPos);

            // Recompensa base por sobrevivir un paso
            float reward = 10f;

            if (newDist > oldDist)
            {
                if (newDist <= 3) reward += 100f;
                else if (newDist <= 7) reward += 40f;
                else reward += 15f;
            }
            else if (newDist < oldDist)
            {
                if (newDist <= 3) reward -= 200f;
                else if (newDist <= 7) reward -= 80f;
                else reward -= 20f;
            }
            else // misma distancia
            {
                if (newDist <= 3) reward -= 100f;
                else if (newDist <= 7) reward += 5f;
                else reward += 10f;
            }

            return reward;
        }

        private int ManhattanDistance(CellInfo a, CellInfo b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        private bool IsTerminalState(CellInfo agent, CellInfo other)
        {
            // El episodio termina si el agente es atrapado
            // Ojo: El límite de pasos (_params.maxSteps) ya lo gestiona DoStep fuera de esta región,
            // pero aquí debemos confirmar la condición de captura.
            if (agent == other)
            {
                Console.WriteLine("Se ha atrapado al agente");
                return true;
            }
            // También comprobamos los pasos por si acaso la lógica externa depende de este bool
            if (CurrentStep >= _params.maxSteps)
            {
                return true;
            }

            return false;
        }

        private CellInfo ApplyAction(CellInfo agentCell, QAction action)
        {
            int nx = agentCell.x;
            int ny = agentCell.y;

            switch (action)
            {
                case QAction.Up: ny += 1; break;
                case QAction.Down: ny -= 1; break;
                case QAction.Right: nx += 1; break;
                case QAction.Left: nx -= 1; break;
                case QAction.Stay: return agentCell;
            }

            CellInfo next = _worldInfo[nx, ny];

            // Si hay muro, se queda quieto y recibe la penalización de ComputeReward
            if (!next.Walkable)
                return agentCell;

            return next;
        }

        private CellInfo MoveOpponent(CellInfo opponent, CellInfo target)
        {
            // Usamos el algoritmo de navegación (A*) proporcionado en Initialize
            // para que el enemigo persiga al agente de forma inteligente.
            if (_navigationAlgorithm == null)
            {
                Console.WriteLine("Oponente no encontrado, referencia null");
                return opponent;
            }

            var path = _navigationAlgorithm.GetPath(opponent, target, 1);

            // Si hay camino, el enemigo avanza al primer paso
            if (path != null && path.Length > 0)
                return path[0];

            return opponent;
        }

    }

}

