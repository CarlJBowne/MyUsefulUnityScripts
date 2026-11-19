using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SLS.SaveFileCore
{
    /// <summary>
    /// An abstract class representing an individual SaveFile of indeterminant Stream Version. It will attempt to load the most recent version of the SaveFile, and if it fails, it will attempt to load the next most recent version, and so on until it either finds a valid SaveFile or determines that no SaveFile exists with the appropriate parameters.
    /// </summary>
    public abstract class SaveFile
    {
        /// <summary>Gets the parameters used to create each file-version instance.</summary>
        public object[] parameters { get; protected set; }

        /// <summary>Gets the supported file-version identifiers and their implementing types; the first entry is the desired version.</summary>
        public abstract Dictionary<string, Type> AllFileVersions { get; }
        /// <summary>Creates a file-version instance using this save file's parameters.</summary>
        /// <param name="T">Concrete file-version type to instantiate.</param>
        /// <returns>The created file-version instance.</returns>
        public virtual FileVersion Create(Type T) => FileVersion.Create(T, parameters);
        /// <summary>Gets the type of the first supported version, which is the version used for saving.</summary>
        public Type DesiredFileVersion => AllFileVersions.First().Value;

        /// <summary>Gets the version instance currently selected for this save file.</summary>
        public FileVersion ActiveFileVersion => active;
        /// <summary>Stores the selected version instance, or <see langword="null"/> when none has been selected yet.</summary>
        private FileVersion active;

        /// <summary>Initializes this save file, selects a matching stored version when possible, and retains the supplied parameters.</summary>
        /// <param name="newParameters">Arguments used to configure the save-file versions.</param>
        public SaveFile(params object[] newParameters)
        {
            EstablishParams(newParameters);
            active = Create(DesiredFileVersion);
            if (active.MatchVersion(out string version).IfFail(out FileOpMessage failReason))
            {
                if (failReason == FileOpMessage.FileVersionMismatch)
                {
                    active = Create(AllFileVersions[version]);
                    if (active.MatchVersion(out _).IfFail(out failReason))
                    {
                        active = Create(DesiredFileVersion);
                    }
                }
                else
                {
                    active = null;
                    foreach (var pair in AllFileVersions)
                    {
                        if (pair.Value == DesiredFileVersion) continue;
                        active = Create(pair.Value);
                        if (!active.MatchVersion(out string loaderVersion).IfFail(out failReason)) return;
                        else if (failReason == FileOpMessage.FileVersionMismatch)
                        {
                            active = Create(AllFileVersions[version]);
                            if (!active.MatchVersion(out _).IfFail(out failReason)) return;
                            else active = null;
                        }
                    }
                    active ??= Create(DesiredFileVersion);
                }
            }

        }

        /// <summary>Loads data from the active version, invoking the configured pre-load and post-load callbacks.</summary>
        public void LoadFromFile()
        {
            if (!active.Valid) return;
            PreLoad();
            active.LoadFromFile();
            PostLoad();
        }

        /// <summary>Saves data using the desired version, replacing an older active version if necessary.</summary>
        public void SaveToFile()
        {
            PreSave();
            if (active.GetType() != DesiredFileVersion)
            {
                active.DeleteFile();
                active = Create(DesiredFileVersion);
            }
            active.SaveToFile();
            PostSave();
        }

        /// <summary>Exports display data from the active file version as the requested type.</summary>
        /// <typeparam name="T">Expected display-data type.</typeparam>
        /// <param name="result">Receives the exported display data.</param>
        /// <exception cref="Exception">No active file version exists, or its display data is not assignable to <typeparamref name="T"/>.</exception>
        public void ExportDisplayData<T>(out T result)
        {
            if (active == null) throw new Exception($"SaveFile with params {parameters} has no FileVersion assigned. Cannot export Menu Display Data.");
            active.ExportDisplayData(out object resultObj);
            if (resultObj is not T) throw new Exception($"SaveFile has FileVersion {active.GetType().Name} which returned an object of type {resultObj.GetType().Name}, which cannot be cast to the requested type {typeof(T).Name}");
            result = (T)resultObj;
        }

        /// <summary>Callback invoked immediately before loading from the active version.</summary>
        public Action PreLoad;
        /// <summary>Callback invoked immediately before saving to the desired version.</summary>
        public Action PreSave;
        /// <summary>Callback invoked after loading from the active version.</summary>
        public Action PostLoad;
        /// <summary>Callback invoked after saving to the desired version.</summary>
        public Action PostSave;

        /// <summary>Stores the parameters used when creating file-version instances.</summary>
        /// <param name="parameters">Configuration arguments to retain.</param>
        public virtual void EstablishParams(params object[] parameters) => this.parameters = parameters;
    }
}
