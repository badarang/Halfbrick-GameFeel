using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class ObjectPooler : MonoSingleton<ObjectPooler>
{
    public class PooledObject
    {
        public GameObject gameObject;
        public bool IsTaken { get; set; }
    }

    [System.Serializable]
    public class PoolInfo
    {
        public GameObject m_prefab = null;
        public int m_instanceCount = 10;
        public bool m_allowIncrease = false;
        public List<PooledObject> m_pooledObjects = new List<PooledObject>();
    }

    public List<PoolInfo> pools = new List<PoolInfo>();

    void Start()
    {
        for (int i = 0; i < pools.Count; ++i)
        {
            CreatePool(pools[i]);
        }
    }

    //Create pooled objects
    void CreatePool(PoolInfo info)
    {
        if (info.m_pooledObjects.Count > 0)
        {
            return;
        }

        for(int i = 0; i < info.m_instanceCount; ++i)
        {
            PooledObject item = new PooledObject();
            item.gameObject = Instantiate(info.m_prefab, transform);
            item.gameObject.SetActive(false);
            item.IsTaken = false;
            info.m_pooledObjects.Add(item);
        }
    }

    //Retrieve pooled object
    public GameObject GetObject(string objectName)
    {
        for(int i = 0; i < pools.Count; ++i)
        {
            if (!pools[i].m_prefab || pools[i].m_prefab.name != objectName)
            {
                continue;
            }

            List<PooledObject> objs = pools[i].m_pooledObjects;
            for(int c = 0; c < objs.Count; ++c)
            {
                if (objs[c].IsTaken == false)
                {
                    objs[c].IsTaken = true;
                    return objs[c].gameObject;
                }
            }
            if(pools[i].m_allowIncrease)
            {
                //Add new object and return it.
                PooledObject item = new PooledObject();
                item.gameObject = Instantiate(pools[i].m_prefab, transform);
                item.gameObject.SetActive(false);
                item.IsTaken = true;
                objs.Add(item);
                return item.gameObject;
            }
        }

        return null;
    }

    //Return to object pooler
    public void ReturnObject(GameObject obj)
    {
        //Return us!
        for (int i = 0; i < pools.Count; ++i)
        {
            List<PooledObject> objs = pools[i].m_pooledObjects;
            for (int c = 0; c < objs.Count; ++c)
            {
                PooledObject pObj = objs[c];
                if (pObj.gameObject == obj)
                {
                    pObj.IsTaken = false;
                    obj.SetActive(false);
                    obj.transform.SetParent(transform);
                }
            }
        }
    }
}
