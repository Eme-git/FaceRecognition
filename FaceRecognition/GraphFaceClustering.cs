using FaceRecognitionDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceRecognitionApp
{
    public class GraphFaceClustering
    {
        private readonly double _similarityThreshold;

        public GraphFaceClustering(double similarityThreshold = 0.6)
        {
            _similarityThreshold = similarityThreshold;
        }

        public List<List<FaceInfo>> Cluster(List<FaceInfo> faces)
        {
            if (faces == null || faces.Count == 0)
                return new List<List<FaceInfo>>();

            Console.WriteLine($"Графовая кластеризация {faces.Count} лиц...");

            // 1. Строим граф смежности
            var adjacencyList = BuildAdjacencyList(faces);

            // 2. Находим связные компоненты (кластеры)
            var clusters = FindConnectedComponents(faces, adjacencyList);

            // 3. Фильтруем слишком маленькие кластеры
            clusters = FilterClusters(clusters, minSize: 1);

            Console.WriteLine($"Найдено {clusters.Count} кластеров");

            return clusters;
        }

        // Построение списка смежности (графа)
        private Dictionary<int, List<int>> BuildAdjacencyList(List<FaceInfo> faces)
        {
            var adjacencyList = new Dictionary<int, List<int>>();
            int n = faces.Count;

            // Инициализируем список смежности для каждой вершины
            for (int i = 0; i < n; i++)
            {
                adjacencyList[i] = new List<int>();
            }

            // Предварительно вычисляем матрицу расстояний для оптимизации
            var distanceMatrix = ComputeDistanceMatrix(faces);

            // Строим рёбра между похожими лицами
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    double distance = distanceMatrix[i, j];

                    if (distance < _similarityThreshold)
                    {
                        // Добавляем ребро в обе стороны (неориентированный граф)
                        adjacencyList[i].Add(j);
                        adjacencyList[j].Add(i);
                    }
                }
            }

            return adjacencyList;
        }

        // Вычисление матрицы расстояний
        private double[,] ComputeDistanceMatrix(List<FaceInfo> faces)
        {
            int n = faces.Count;
            var matrix = new double[n, n];

            // Заполняем матрицу расстояний
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    double distance = FaceRecognition.FaceDistance(
                        faces[i].Encoding,
                        faces[j].Encoding);

                    matrix[i, j] = distance;
                    matrix[j, i] = distance; 
                }

                matrix[i, i] = 0.0; // Расстояние до себя = 0
            }

            return matrix;
        }

        // Поиск связных компонент графа (DFS)
        private List<List<FaceInfo>> FindConnectedComponents(
            List<FaceInfo> faces,
            Dictionary<int, List<int>> adjacencyList)
        {
            int n = faces.Count;
            var visited = new bool[n];
            var components = new List<List<FaceInfo>>();

            for (int i = 0; i < n; i++)
            {
                if (!visited[i])
                {
                    // Начинаем новый компонент связности
                    var component = new List<FaceInfo>();

                    // Обход в глубину (DFS)
                    DFS(i, faces, adjacencyList, visited, component);

                    if (component.Count > 0)
                    {
                        components.Add(component);
                    }
                }
            }

            return components;
        }

        // Обход в глубину
        private void DFS(int node,
                        List<FaceInfo> faces,
                        Dictionary<int, List<int>> adjacencyList,
                        bool[] visited,
                        List<FaceInfo> component)
        {
            var stack = new Stack<int>();
            stack.Push(node);

            while (stack.Count > 0)
            {
                int current = stack.Pop();

                if (!visited[current])
                {
                    visited[current] = true;
                    component.Add(faces[current]);

                    // Добавляем всех соседей
                    foreach (var neighbor in adjacencyList[current])
                    {
                        if (!visited[neighbor])
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            }
        }

        // Фильтрация кластеров по размеру
        private List<List<FaceInfo>> FilterClusters(List<List<FaceInfo>> clusters, int minSize)
        {
            return clusters.Where(c => c.Count >= minSize).ToList();
        }
    }
}
