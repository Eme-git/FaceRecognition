using FaceRecognitionDotNet;
using FaceRecognitionApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceRecognitionApp
{
    public class TwoStageFaceClustering
    {
        private readonly double _strictThreshold;
        private readonly double _mergeThreshold;

        public TwoStageFaceClustering(double strictThreshold = 0.4,
                                      double mergeThreshold = 0.5)
        {
            _strictThreshold = strictThreshold;
            _mergeThreshold = mergeThreshold;
        }

        public List<List<FaceInfo>> Cluster(List<FaceInfo> faces)
        {
            // Этап 1: Строгая кластеризация (каждое лицо сравнивается с каждым)
            var initialClusters = StrictPairwiseClustering(faces);

            // Этап 2: Вычисляем медоиды для каждого кластера
            var clusterMedoids = ComputeClusterMedoids(initialClusters);

            // Этап 3: Объединяем похожие кластеры
            var mergedClusters = MergeClustersByMedoids(initialClusters, clusterMedoids);

            return mergedClusters;
        }

        private List<List<FaceInfo>> StrictPairwiseClustering(List<FaceInfo> faces)
        {
            int n = faces.Count;
            var visited = new bool[n];
            var clusters = new List<List<FaceInfo>>();

            for (int i = 0; i < n; i++)
            {
                if (visited[i]) continue;

                var cluster = new List<FaceInfo> { faces[i] };
                visited[i] = true;

                // Ищем ВСЕ похожие лица с жёстким порогом
                for (int j = 0; j < n; j++)
                {
                    if (visited[j]) continue;

                    double distance = FaceRecognition.FaceDistance(
                        faces[i].Encoding,
                        faces[j].Encoding);

                    if (distance < _strictThreshold) // Жёсткий порог!
                    {
                        cluster.Add(faces[j]);
                        visited[j] = true;
                    }
                }

                clusters.Add(cluster);
            }

            return clusters;
        }

        private List<FaceInfo> ComputeClusterMedoids(List<List<FaceInfo>> clusters)
        {
            var medoids = new List<FaceInfo>();

            foreach (var cluster in clusters)
            {
                // Находим медоид - лицо с минимальной суммой расстояний до других
                FaceInfo medoid = null;
                double minTotalDistance = double.MaxValue;

                foreach (var candidate in cluster)
                {
                    double totalDist = 0;
                    foreach (var other in cluster)
                    {
                        if (candidate != other)
                        {
                            totalDist += FaceRecognition.FaceDistance(
                                candidate.Encoding,
                                other.Encoding);
                        }
                    }

                    if (totalDist < minTotalDistance)
                    {
                        minTotalDistance = totalDist;
                        medoid = candidate;
                    }
                }

                medoids.Add(medoid);
            }

            return medoids;
        }

        private List<List<FaceInfo>> MergeClustersByMedoids(
            List<List<FaceInfo>> clusters,
            List<FaceInfo> medoids)
        {
            int n = clusters.Count;
            var visited = new bool[n];
            var mergedClusters = new List<List<FaceInfo>>();

            for (int i = 0; i < n; i++)
            {
                if (visited[i]) continue;

                var mergedCluster = new List<FaceInfo>(clusters[i]);
                visited[i] = true;

                // Ищем кластеры с похожими медоидами
                for (int j = i + 1; j < n; j++)
                {
                    if (visited[j]) continue;

                    double distance = FaceRecognition.FaceDistance(
                        medoids[i].Encoding,
                        medoids[j].Encoding);

                    if (distance < _mergeThreshold) // Мягкий порог для объединения
                    {
                        mergedCluster.AddRange(clusters[j]);
                        visited[j] = true;
                    }
                }

                mergedClusters.Add(mergedCluster);
            }

            return mergedClusters;
        }
    }
}
