using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using System;
using System.Linq;
using System.Windows.Input;

namespace ScalableEmitterEditorPlugin
{
    public class EmitterStackItemData : BaseViewModel
    {

        #region -- Fields --

        public object EmitterItemObj;
        public object EvaluatorObj;

        private FrostyPropertyGrid propertyGrid;
        private bool isEmitterRoot;
        private bool processorSelected;
        private bool evaluatorSelected;
        private string processorText;
        private string evaluatorText;
        private bool evaluatorVisible;

        #endregion

        #region -- Properties --

        public bool IsEmitterRoot
        {
            get
            {
                return isEmitterRoot;
            }
            set
            {
                if (isEmitterRoot != value)
                {
                    isEmitterRoot = value;
                    RaisePropertyChanged("IsEmitterRoot");
                }
            }
        }
        public bool ProcessorSelected
        {
            get
            {
                return processorSelected;
            }
            set
            {
                if (processorSelected != value)
                {
                    processorSelected = value;
                    RaisePropertyChanged("ProcessorSelected");
                }
            }
        }
        public bool EvaluatorSelected
        {
            get
            {
                return evaluatorSelected;
            }
            set
            {
                if (evaluatorSelected != value)
                {
                    evaluatorSelected = value;
                    RaisePropertyChanged("EvaluatorSelected");
                }
            }
        }
        public string ProcessorText
        {
            get
            {
                return processorText;
            }
            set
            {
                if (processorText != value)
                {
                    processorText = value;
                    RaisePropertyChanged("ProcessorText");
                }
            }
        }
        public string EvaluatorText
        {
            get
            {
                return evaluatorText;
            }
            set
            {
                if (evaluatorText != value)
                {
                    evaluatorText = value;
                    RaisePropertyChanged("EvaluatorText");
                }
            }
        }
        public bool EvaluatorVisible
        {
            get
            {
                return evaluatorVisible;
            }
            set
            {
                if (evaluatorVisible != value)
                {
                    evaluatorVisible = value;
                    RaisePropertyChanged("EvaluatorVisible");
                }
            }
        }

        #endregion

        #region -- Constructors --

        public ICommand CopyCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand InsertPasteCommand { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="EmitterStackItemData"/> class with a referenced object.
        /// </summary>
        /// <param name="obj">The processor or evaluator that this item represents</param>
        /// <param name="isRoot">Is this item the emitter base?</param>
        /// <param name="pg">The property grid to be updated</param>
        public EmitterStackItemData(dynamic obj, bool isRoot, FrostyPropertyGrid pg, Action<object> refreshAction)
        {
            propertyGrid = pg;
            EmitterItemObj = obj;
            isEmitterRoot = isRoot;
            ProcessorSelected = false;
            EvaluatorSelected = false;
            ProcessorText = "Processor";
            EvaluatorText = "Evaluator";
            EvaluatorVisible = false;

            if (isEmitterRoot)
            {
                ProcessorText = "Emitter Base";
            }
            else
            {
                ProcessorText = CleanUpName(((dynamic)EmitterItemObj).__Id);
                if (Utils.DoesPropertyExist(EmitterItemObj, "Pre"))
                {
                    EvaluatorObj = ((dynamic)EmitterItemObj).Pre.Internal;
                    if (EvaluatorObj != null)
                    {
                        EvaluatorText = CleanUpName(((dynamic)EvaluatorObj).__Id);
                        EvaluatorText += $" ({ CleanUpName(((dynamic)EmitterItemObj).EvaluatorInput.ToString()) })";
                        EvaluatorVisible = true;
                    }
                }
            }

            CopyCommand = new RelayCommand((_) => {
                FrostyClipboard.Current.SetData(new PointerRef(EmitterItemObj));
            });

            DeleteCommand = new RelayCommand((_) => {
                dynamic prevProcessor = pg.Asset.Objects.FirstOrDefault(o => {
                    try {
                        return ((dynamic)o).NextProcessor?.Internal == EmitterItemObj;
                    }
                    catch {
                        try {
                            return ((dynamic)o).RootProcessor?.Internal == EmitterItemObj;
                        }
                        catch { return false; }
                    }
                });

                if (prevProcessor != null) {
                    try {
                        prevProcessor.NextProcessor = ((dynamic)EmitterItemObj).NextProcessor;
                    }
                    catch {
                        try {
                            prevProcessor.RootProcessor = ((dynamic)EmitterItemObj).NextProcessor;
                        }
                        catch {
                            return;
                        }
                    }
                    propertyGrid.Modified = true;
                }
                
            } + refreshAction);

            InsertPasteCommand = new RelayCommand((_) => {
                if (FrostyClipboard.Current.HasData) {

                    dynamic clipboardData = FrostyClipboard.Current.GetData(pg.Asset, App.AssetManager.GetEbxEntry(pg.Asset.FileGuid));
                    clipboardData = clipboardData?.Internal;

                    if (clipboardData is null)
                        return;

                    try {
                        clipboardData.NextProcessor = ((dynamic)EmitterItemObj).NextProcessor;
                        ((dynamic)EmitterItemObj).NextProcessor = new PointerRef((object)clipboardData);
                    }
                    catch {
                        try {
                            clipboardData.NextProcessor = ((dynamic)EmitterItemObj).RootProcessor;
                            ((dynamic)EmitterItemObj).RootProcessor = new PointerRef((object)clipboardData);
                        }
                        catch {
                            return;
                        }
                    }

                    propertyGrid.Modified = true;
                }
            }
            + refreshAction);
        }

        #endregion

        string CleanUpName(string name)
        {
            if (name.EndsWith("Data"))
                return name.Remove(name.Length - 4);
            else if (name.StartsWith("Ef"))
                return name.Remove(0, 2);
            return name;
        }

    }
}
