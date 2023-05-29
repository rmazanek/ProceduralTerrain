using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

namespace ProceduralTerrain
{
    public class ThreadedDataRequester : MonoBehaviour
    {
        static ThreadedDataRequester instance;
        Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();
        private void Awake() {
            instance = FindObjectOfType<ThreadedDataRequester>();
        }
        public static void RequestData(Func<object> generateData, Action<object> callback)
        {
            ThreadStart threadStart = delegate 
            {
                instance.DataThread(generateData, callback);
            };

            new Thread(threadStart).Start();
        }
        private void DataThread(Func<object> generateData, Action<object> callback)
        {
            object data = generateData();
            lock(dataQueue)
            {
                dataQueue.Enqueue(new ThreadInfo (callback, data));
            }
        }
        private void Update()
        {
            CheckForNewDataOnThread();
        }
        private void CheckForNewDataOnThread()
        {
            if (dataQueue.Count <= 0) return;
            for (int i = 0; i < dataQueue.Count; i++)
            {
                ThreadInfo threadInfo = dataQueue.Dequeue();
                threadInfo.Callback(threadInfo.Parameter);
            }
        }
    }
    struct ThreadInfo
    {
        public readonly Action<object> Callback;
        public readonly object Parameter;
        public ThreadInfo(Action<object> callback, object parameter)
        {
            this.Callback = callback;
            this.Parameter = parameter;
        }
    }
}
