namespace FaceRecognitionApp
{
    public class FaceCluster
    {
        private static GraphFaceClustering _clusterizer = new GraphFaceClustering();

        public List<FaceInfo> Faces { get; set; }

        public FaceCluster(List<FaceInfo> faces)
        {
            Faces = faces;
        }

        public static List<FaceCluster> FindClusters(List<FaceInfo> faces)
        {
            var persons = _clusterizer.Cluster(faces);
            var result = new List<FaceCluster>();

            foreach (var person in persons)
            {
                var cluster = new FaceCluster(person);

                result.Add(cluster);
            }

            return result;
        }
    }
}
