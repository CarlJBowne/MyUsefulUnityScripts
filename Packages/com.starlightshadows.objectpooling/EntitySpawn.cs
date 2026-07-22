using System.Collections.Generic;
using UnityEngine;
using Utilities.ObjectPooling;

namespace Utilities
{
    public class EntitySpawn : MonoBehaviour
    {
        public Spawnable prefab;
        public bool usePool = false;
        public float loadDistance = 30;
        public float offScreenDistance = 40;
        public float unloadDistance = 70;
        public bool measureFromSpawn = false;

        Spawnable active;
        bool actionDelayed = false;
        public bool hidden = false;
        ObjectPool pool;

        private void Awake()
        {
            //if (Gameplay.GameState != Gameplay.GameStates.Active) return;
            actionDelayed = true;
            UpdateProxy.QueueUpdate(() =>
            {
                actionDelayed = false;
                if (usePool)
                {
                    if (!activePools.TryGetValue(prefab.gameObject, out pool))
                    {
                        pool = ObjectPool<Spawnable>.NEW(prefab, 3, true);
                        activePools.Add(prefab.gameObject, pool);
                        pool.Initialize();
                    }
                }
                if (Distance < loadDistance) AttemptLoad();
            }, "EntitySpawn", true);
        }

        private void FixedUpdate()
        {
            if (actionDelayed) return;
            float distance = Distance;

            if ((active == null || active.Ready || (!active.Active && usePool)) && !hidden)
            {
                if (distance < loadDistance) AttemptLoad();
            }
            else if (active.Active) 
            {
                if (distance > unloadDistance)
                {
                    active.Despawn();
                    active.ResetAlterations();
                }
                else if (distance > offScreenDistance)
                {
                    active.gameObject.SetActive(false);
                    active.Reserved = true;
                    hidden = true;
                }
            }
            else if (hidden)
            {
                if (distance < loadDistance)
                {
                    active.gameObject.SetActive(true);
                    active.Reserved = false;
                    hidden = false;
                }
                else if (distance > unloadDistance)
                {
                    active.ResetAlterations();
                    active.Reserved = false;
                    hidden = false;
                }
            }
        }

        float Distance => Vector3.Distance(PlayerPosition.position,
                (measureFromSpawn || active == null) ? transform.position : active.transform.position);

        void AttemptLoad()
        {
            actionDelayed = true;
            UpdateProxy.QueueUpdate(() =>
            {
                actionDelayed = false;
                if (active == null || active.Ready || (!active.Active && usePool))
                {
                    if (active != null && active.Ready) active.Spawn(transform);
                    else if (usePool) active = pool.Pump(transform);
                    else
                    {
                        active = Spawnable.Instantiate(prefab.gameObject, transform);
                        active.Spawn(transform);
                    }
                    active.SendMessage("OnSpawn");
                }
            }, "EntitySpawn", true);
        }

        private void OnDestroy()
        {
            if (active != null)
            {
                active.Despawn();
                active.Reserved = false;
                active.ResetAlterations();
            }
        }


        public static Transform PlayerPosition;
        public static Dictionary<GameObject, ObjectPool> activePools = new();

    }
}