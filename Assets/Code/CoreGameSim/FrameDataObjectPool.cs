using System.Collections.Generic;

namespace Sim
{
    public class FrameDataObjectPool<TFrameData> where TFrameData: new()
    {
        protected const int c_iDefaultStartCapacity = 8;

        protected Queue<TFrameData> m_fdaFrameDataQueue ;


        public FrameDataObjectPool()
        {
            m_fdaFrameDataQueue = new Queue<TFrameData>(c_iDefaultStartCapacity);

            for(int i = 0; i < c_iDefaultStartCapacity; i++)
            {
                m_fdaFrameDataQueue.Enqueue(new TFrameData());
            }
        }

        public TFrameData GetFrameData()
        {
            if(m_fdaFrameDataQueue.Count != 0)
            {
                return m_fdaFrameDataQueue.Dequeue();
            }
            else
            {
                return new TFrameData();
            }
        }

        public void ReturnFrameData(in TFrameData fdaDataToReturn)
        {
            m_fdaFrameDataQueue.Enqueue(fdaDataToReturn);
        }
    }
}