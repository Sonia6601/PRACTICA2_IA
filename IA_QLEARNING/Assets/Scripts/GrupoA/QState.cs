using NavigationDJIA.World;
using System; // Necesario para Math.Sign
using UnityEngine;

/// <summary>
/// TODO(alumno):
/// Define el "estado" que usará la Tabla Q para identificar cada situación del agente.
/// 
/// El estado debe contener toda la información necesaria para que el agente pueda
/// tomar decisiones informadas. Tú decides qué características incluir según lo
/// que consideres relevante para resolver el problema.
/// 
/// Ejemplos típicos de información que puede formar un estado:
///   - Posición del agente en la grid.
///   - Posición del otro personaje (enemigo).
///   - Distancia relativa entre agente y enemigo.
///   - Si hay muros en direcciones cercanas.
///   - Cualquier otro dato que consideres útil.
/// 
/// En este ejercicio te damos un ejemplo simple basado únicamente en las posiciones
/// del agente y del oponente. Puedes usarlo tal cual o ampliarlo.
/// 
/// IMPORTANTE: 
///  El estado debe poder convertirse a una clave única (string) mediante ToKey(),
///  ya que esa clave se usará como índice en la TablaQ y en el archivo CSV.
/// </summary>

namespace GrupoA
{
    public sealed class QState
    {
        public int DirX { get; }
        public int DirY { get; }
        public int DistanceValues { get; }
        public bool WallUp { get; }
        public bool WallDown { get; }
        public bool WallLeft { get; }
        public bool WallRight { get; }

        // Dirección con más espacio libre
        public int EscapeDirX { get; }
        public int EscapeDirY { get; }

        public QState(CellInfo agent, CellInfo other,
            bool wallUp, bool wallDown, bool wallLeft, bool wallRight,
            int escapeDirX, int escapeDirY)  // parámetros de la función
        {
            //calculo de distancia
            DirX = Math.Sign(other.x - agent.x); 
            DirY = Math.Sign(other.y - agent.y);

            int dist = Math.Abs(other.x - agent.x) + Math.Abs(other.y - agent.y); //calculo de la distancia
            DistanceValues = dist <= 3 ? 0 : dist <= 7 ? 1 : 2; //si es <=3, es 0, si es <= 7, entonces 1, y si no 2

            WallUp = wallUp;
            WallDown = wallDown;
            WallLeft = wallLeft;
            WallRight = wallRight;

            EscapeDirX = escapeDirX;
            EscapeDirY = escapeDirY;
        }

        public string ToKey() //Transcripcion de los datos del estado para la tabla Q
        {
            return $"{DirX}|{DirY}|{DistanceValues}|" +
                   $"{(WallUp ? 1 : 0)}{(WallDown ? 1 : 0)}{(WallLeft ? 1 : 0)}{(WallRight ? 1 : 0)}|" +
                   $"{EscapeDirX}|{EscapeDirY}";
        }
    }
}