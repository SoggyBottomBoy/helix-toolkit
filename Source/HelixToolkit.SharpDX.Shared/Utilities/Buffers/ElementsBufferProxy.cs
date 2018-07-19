﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using SharpDX;
using SharpDX.Direct3D11;
using System.Collections.Generic;

using SDX11 = SharpDX.Direct3D11;
#if !NETFX_CORE

namespace HelixToolkit.Wpf.SharpDX.Utilities
#else
namespace HelixToolkit.UWP.Utilities
#endif
{
    using Extensions;
    using Render;

    /// <summary>
    ///
    /// </summary>
    public interface IElementsBufferProxy : IBufferProxy
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        void UploadDataToBuffer<T>(DeviceContextProxy context, IList<T> data, int count) where T : struct;

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="offset"></param>
        /// <param name="minBufferCount">Used to initialize a buffer which size is Max(count, minBufferCount). Only used in dynamic buffer.</param>
        void UploadDataToBuffer<T>(DeviceContextProxy context, IList<T> data, int count, int offset, int minBufferCount = default(int)) where T : struct;

        /// <summary>
        /// <see cref="DisposeObject.DisposeAndClear"/>
        /// </summary>
        void DisposeAndClear();
    }

    /// <summary>
    ///
    /// </summary>
    public sealed class ImmutableBufferProxy : BufferProxyBase, IElementsBufferProxy
    {
        /// <summary>
        ///
        /// </summary>
        public ResourceOptionFlags OptionFlags { private set; get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="structureSize"></param>
        /// <param name="bindFlags"></param>
        /// <param name="optionFlags"></param>
        public ImmutableBufferProxy(int structureSize, BindFlags bindFlags, ResourceOptionFlags optionFlags = ResourceOptionFlags.None)
            : base(structureSize, bindFlags)
        {
            OptionFlags = optionFlags;
        }

        /// <summary>
        /// <see cref="IElementsBufferProxy.UploadDataToBuffer{T}(DeviceContextProxy, IList{T}, int)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        public void UploadDataToBuffer<T>(DeviceContextProxy context, IList<T> data, int count) where T : struct
        {
            UploadDataToBuffer<T>(context, data, count, 0);
        }

        /// <summary>
        /// <see cref="IElementsBufferProxy.UploadDataToBuffer{T}(DeviceContextProxy, IList{T}, int, int, int)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="offset"></param>
        /// <param name="minBufferCount">This is not being used in ImmutableBuffer</param>
        public void UploadDataToBuffer<T>(DeviceContextProxy context, IList<T> data, int count, int offset, int minBufferCount = default(int)) where T : struct
        {
            RemoveAndDispose(ref buffer);
            ElementCount = count;
            if(count == 0)
            {
                return;
            }
            var buffdesc = new BufferDescription()
            {
                BindFlags = this.BindFlags,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = this.OptionFlags,
                SizeInBytes = StructureSize * count,
                StructureByteStride = StructureSize,
                Usage = ResourceUsage.Immutable
            };
            buffer = Collect(Buffer.Create(context, data.GetArrayByType(), buffdesc));
        }
    }

    /// <summary>
    ///
    /// </summary>
    public sealed class DynamicBufferProxy : BufferProxyBase, IElementsBufferProxy
    {
        /// <summary>
        ///
        /// </summary>
        public ResourceOptionFlags OptionFlags { private set; get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="structureSize"></param>
        /// <param name="bindFlags"></param>
        /// <param name="optionFlags"></param>
        public DynamicBufferProxy(int structureSize, BindFlags bindFlags, ResourceOptionFlags optionFlags = ResourceOptionFlags.None)
            : base(structureSize, bindFlags)
        {
            this.OptionFlags = optionFlags;
        }

        /// <summary>
        /// <see cref="IElementsBufferProxy.UploadDataToBuffer{T}(DeviceContextProxy, IList{T}, int)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        public void UploadDataToBuffer<T>(DeviceContextProxy context, IList<T> data, int count) where T : struct
        {
            UploadDataToBuffer<T>(context, data, count, 0);
        }

        /// <summary>
        /// <see cref="IElementsBufferProxy.UploadDataToBuffer{T}(DeviceContextProxy, IList{T}, int, int, int)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="offset"></param>
        /// <param name="minBufferCount">Used to create a dynamic buffer with size of Max(count, minBufferCount).</param>
        public void UploadDataToBuffer<T>(DeviceContextProxy context, IList<T> data, int count, int offset, int minBufferCount = default(int)) where T : struct
        {
            ElementCount = count;
            if (count == 0)
            {
                return;
            }
            else if (buffer == null || buffer.Description.SizeInBytes < StructureSize * count)
            {
                RemoveAndDispose(ref buffer);
                var buffdesc = new BufferDescription()
                {
                    BindFlags = this.BindFlags,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = this.OptionFlags,
                    SizeInBytes = StructureSize * System.Math.Max(count, minBufferCount),
                    StructureByteStride = StructureSize,
                    Usage = ResourceUsage.Dynamic
                };
                //buffer = Collect(SDX11::Buffer.Create(context, data.GetArrayByType(), buffdesc));
                buffer = Collect(new Buffer(context, buffdesc));
            }
            context.MapSubresource(this.buffer, MapMode.WriteNoOverwrite, MapFlags.None, out DataStream stream);
            using (stream)
            {
                stream.WriteRange(data.GetArrayByType(), offset, count);                    
            }
            context.UnmapSubresource(this.buffer, 0);
        }
    }
}