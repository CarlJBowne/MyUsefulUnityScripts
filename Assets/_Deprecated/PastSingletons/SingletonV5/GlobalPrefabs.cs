#define AYellowPaper

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using S = ISingleton<GlobalPrefabs>;

[CreateAssetMenu(fileName = "Global Prefabs", menuName = "Global Prefabs", order = 0)]
public class GlobalPrefabs : ScriptableObject, ISingleton<GlobalPrefabs>
{
    #region Boiler Plate
    private static GlobalPrefabs _instance;
    private ISingleton<GlobalPrefabs> Interface = _instance;
    public static GlobalPrefabs Get() => S.Get(ref _instance);
    public static bool TryGet(ref GlobalPrefabs _instance) => S.TryGet(Get, out _instance);

    void Awake() => Interface.Initialize(ref _instance);
    void OnEnable() => Interface.Initialize(ref _instance);

    void ISingleton<GlobalPrefabs>.OnInitialize()
    {
#if AYellowPaper
#else
        dictionary = new();
        for (int i = 0; i < NamedPrefabs.Length; i++)
            dictionary.Add(PrefabNames[i], NamedPrefabs[i]);
#endif
    }

    #endregion Boiler Plate


    public List<Singleton> singletons;
    public static List<Singleton> Singletons => Get().singletons;

#if AYellowPaper
    [SerializeField] AYellowpaper.SerializedCollections.SerializedDictionary<string, GameObject> namedDictionary;
    public static AYellowpaper.SerializedCollections.SerializedDictionary<string, GameObject> NamedDictionary => Get().namedDictionary;
#else

    [SerializeField] GameObject[] NamedPrefabs;
    [SerializeField] string[] PrefabNames;
    private Dictionary<string, GameObject> dictionary;

#endif

    public GameObject this[string name] => Get().namedDictionary[name];
    public static GameObject NamedPrefab(string name) => Get().namedDictionary[name];
    public static bool TryNamedPrefab(string name, out GameObject result) => Get().namedDictionary.TryGetValue(name, out result);








}