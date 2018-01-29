using System;
using System.Collections.Generic;
using VVVV.PluginInterfaces.V2;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using System.IO;

namespace VVVV.DX11.Nodes.Spreaded
{
    public abstract class FileTextureLoadTask<T> : DX11AbstractLoadTask<T> where T : IDX11Resource
    {
        public int Slice { get; protected set; }
        protected string path;

        public FileTextureLoadTask(DX11RenderContext context, int slice, string path)
            : base(context)
        {
            this.Slice = slice;
            this.path = path;
        }

        public override void Dispose()
        {
            if (this.Resource != null) { this.Resource.Dispose(); }
        }
    }

    public abstract class FileTextureBaseNode<T> : IPluginEvaluate, IDX11ResourceHost, IDisposable where T : IDX11Resource
    {
        [Input("Filename", StringType = StringType.Filename)]
        protected IDiffSpread<string> FInPath;

        [Input("Load In Background", IsSingle = true)]
        protected ISpread<bool> FInBGLoad;

        [Input("Reload", IsBang = true)]
        protected ISpread<bool> FInReload;

        [Input("Keep In Memory", Visibility = PinVisibility.Hidden)]
        protected ISpread<bool> FInKeep;

        [Input("No Mips", Visibility = PinVisibility.Hidden)]
        protected IDiffSpread<bool> FInNoMips;

        [Output("Texture Out", Order = 1)]
        protected ISpread<DX11Resource<T>> FTextureOutput;

        [Output("Is Valid", Order = 500)]
        protected ISpread<bool> FValid;

        //protected static Dictionary<string, T> respool = new Dictionary<string, T>();

        private bool FInvalidate;
        private bool FDestroyed;

        protected abstract bool Load(DX11RenderContext context, string path, out T result);

        protected abstract FileTextureLoadTask<T> GetTask(DX11RenderContext context, string path, int slice);

        private List<FileTextureLoadTask<T>> tasks = new List<FileTextureLoadTask<T>>();

        private object m_lock = new object();

        HashSet<int> FSkipIndex = new HashSet<int>();

        public void Evaluate(int SpreadMax)
        {
            FSkipIndex.Clear();

            bool anyReload = false;
            foreach (var entry in this.FInReload)
            {
                if (entry)
                {
                    anyReload = true;
                    break;
                }
            }

            bool needsReload = this.FInPath.IsChanged || this.FInNoMips.IsChanged;

            if (needsReload || anyReload)
            {
                //Kill old resources
                for (int i = 0; i < this.FTextureOutput.SliceCount; i++)
                {
                    if (needsReload || FInReload[i])
                    {
                        if (this.FTextureOutput[i] != null) { this.FTextureOutput[i].Dispose(); }
                    }
                }

                foreach (FileTextureLoadTask<T> task in this.tasks)
                {
                    task.MarkForAbort();
                }


                this.FTextureOutput.SliceCount = SpreadMax;
                this.FValid.SliceCount = SpreadMax;

                for (int i = 0; i < SpreadMax; i++)
                {
                    if (needsReload || FInReload[i])
                    {
                        this.FTextureOutput[i] = new DX11Resource<T>();
                        this.FValid[i] = false;
                    }
                    else
                    {
                        FSkipIndex.Add(i);
                    }
                }

                this.FInvalidate = true;
            }
        }

        protected virtual void SliceCountchanged(int slicecount) { }
        protected abstract void LoadInfo(int slice, string path);
        protected abstract void MarkInvalid(int slice);

        public void Update(DX11RenderContext context)
        {
            if (this.FInvalidate || this.FDestroyed)
            {
                for (int i = 0; i < this.FInPath.SliceCount; i++)
                {
                    if(this.FSkipIndex.Contains(i))
                    {
                        FSkipIndex.Remove(i);
                        continue;
                    }
                    T result;

                    if (File.Exists(this.FInPath[i]))
                    {
                        if (this.FInBGLoad[0])
                        {

                            FileTextureLoadTask<T> task = this.GetTask(context, this.FInPath[i], i);
                            task.StatusChanged += new TaskStatusChangedDelegate(task_StatusChanged);
                            this.tasks.Add(task);
                        }
                        else
                        {

                            bool res = this.Load(context, this.FInPath[i], out result);
                            this.FTextureOutput[i][context] = result;
                            this.FValid[i] = res;
                        }
                    }
                    else
                    {
                        this.FTextureOutput[i][context] = default(T);
                        this.FValid[i] = false;
                    }
                }

                this.FInvalidate = false;
                this.FDestroyed = false;
            }


        }

        void task_StatusChanged(IDX11ScheduledTask task)
        {
            /*if (task.Status == eDX11SheduleTaskStatus.Completed
                || task.Status == eDX11SheduleTaskStatus.Aborted
                || task.Status == eDX11SheduleTaskStatus.Error)
            {
                this.tasks.Remove(task);
            }

            if (task.Status == eDX11SheduleTaskStatus.Completed)
            {
                this.FTextureOutput[task.
            }*/
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
            if (force || !this.FInKeep[0])
            {
                for (int i = 0; i < this.FTextureOutput.SliceCount; i++)
                {
                    this.FTextureOutput[i].Dispose(context);
                }

                foreach (FileTextureLoadTask<T> task in this.tasks)
                {
                    task.MarkForAbort();
                }
                this.tasks.Clear();

                this.FDestroyed = true;
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            this.FTextureOutput.SafeDisposeAll();
        }
        #endregion
    }
}