#region Copyright notice and license

// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
// http://github.com/jskeet/dotnet-protobufs/
// Original C++/Java/Python code:
// http://code.google.com/p/protobuf/
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.IO;
using System.Collections.Generic;
using Google.ProtocolBuffers.Compiler.PluginProto;
using Google.ProtocolBuffers.DescriptorProtos;

namespace Google.ProtocolBuffers.ProtoGen
{
    /// <summary>
    /// Entry point for the Protocol Buffers generator.
    /// </summary>
    internal class Program
    {
        internal static int Main(string[] args)
        {
            try
            {
                // Hack to make sure everything's initialized
                DescriptorProtoFile.Descriptor.ToString();
                GeneratorOptions options = new GeneratorOptions {Arguments = args};

                IList<string> validationFailures;
                if (!options.TryValidate(out validationFailures))
                {
                    // We've already got the message-building logic in the exception...
                    InvalidOptionsException exception = new InvalidOptionsException(validationFailures);
                    Console.WriteLine(exception.Message);
                    return 1;
                }

                var request = new CodeGeneratorRequest.Builder();
                foreach (string inputFile in options.InputFiles)
                {
                    ExtensionRegistry extensionRegistry = ExtensionRegistry.CreateInstance();
                    CSharpOptions.RegisterAllExtensions(extensionRegistry);
                    using (Stream inputStream = File.OpenRead(inputFile))
                    {
                        var fileSet = FileDescriptorSet.ParseFrom(inputStream, extensionRegistry);
                        foreach (var fileProto in fileSet.FileList)
                        {
                            request.AddFileToGenerate(fileProto.Name);
                            request.AddProtoFile(fileProto);
                        }
                    }
                }

                Generator generator = Generator.CreateGenerator(options);
                var response = new CodeGeneratorResponse.Builder();
                generator.Generate(request.Build(), response);
                if (response.HasError)
                {
                    throw new Exception(response.Error);
                }
                foreach (var file in response.FileList)
                {
                    File.WriteAllText(file.Name, file.Content);
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: {0}", e.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine("Detailed exception information: {0}", e);
                return 1;
            }
        }
    }
}