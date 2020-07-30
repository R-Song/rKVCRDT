using System;
using Xunit;
using RAC;
using System.Collections.Generic;
using System.Reflection;

namespace RACTests
{
    public class APITest : IDisposable
    {

        string typeCode = "gc";
        string typeName = "GCounter";

        public APITest()
        {
            API.typeCodeList = new Dictionary<string, Type>();
            API.typeList = new Dictionary<Type, CRDTypeInfo>();
            API.converterList = new Dictionary<string, (API.StringToType, API.TypeToString)>();

            API.AddNewType(typeName, typeCode);
        }

        void IDisposable.Dispose()
        {
            API.typeCodeList = new Dictionary<string, Type>();
            API.typeList = new Dictionary<Type, CRDTypeInfo>();
            API.converterList = new Dictionary<string, (API.StringToType, API.TypeToString)>();
        }

        [Fact]
        public void TestAddNewTypeSuccess()
        {
            Type gctype;
            CRDTypeInfo gctypeinfo;

            Assert.True(API.typeCodeList.TryGetValue(typeCode, out gctype));
            Assert.Equal(gctype, typeof(RAC.Operations.GCounter));

            Assert.True(API.typeList.TryGetValue(gctype, out gctypeinfo));
            Assert.Equal(gctypeinfo.type, typeof(RAC.Operations.GCounter));
        }

        [Fact]
        public void TestAddNewTypeFail()
        {

            string typeCode = "gcMistake";
            string typeName = "GCounterMistake";

            API.AddNewType(typeName, typeCode);

            Type gctype;

            Assert.False(API.typeCodeList.TryGetValue(typeCode, out gctype));
        }

        [Fact]
        public void TestAddNewAPISuccess()
        {
            Type gctype;
            CRDTypeInfo gctypeinfo;

            Assert.True(API.typeCodeList.TryGetValue(typeCode, out gctype));
            Assert.Equal(gctype, typeof(RAC.Operations.GCounter));
            
            Assert.True(API.typeList.TryGetValue(gctype, out gctypeinfo));
            Assert.Equal(gctypeinfo.type, typeof(RAC.Operations.GCounter));

            API.AddNewAPI("GCounter", "GetValue", "g", "");

            MethodInfo method;

            Assert.True(gctypeinfo.methodsList.TryGetValue("g", out method)); 
            Assert.Equal(method, gctype.GetMethod("GetValue"));
        }

        [Fact]
        public void TestAddNewAPIFail()
        {   
            Type gctype;
            CRDTypeInfo gctypeinfo;

            API.typeCodeList.TryGetValue(typeCode, out gctype);
            Assert.True(API.typeList.TryGetValue(gctype, out gctypeinfo));

            API.AddNewAPI("GCounter", "GetValueMistake", "g", "");
            
            MethodInfo method;

            Assert.False(gctypeinfo.methodsList.TryGetValue("g", out method)); 
        }

        [Fact]
        public void TestAddNewConverter()
        {
            API.StringToType stt = delegate(string x) { return (object)x; };
            API.TypeToString tts = delegate(object x) { return ((string)x); };

            API.AddConverter("test", stt, tts);

            object o = API.GetToTypeConverter("test")("Hello World");

            Assert.Equal("Hello World", API.GetToStringConverter("test")(o));

        }

        [Fact]
        public void TestAddNewAPIWithParam()
        {
            API.StringToType stt = delegate(string x) { return (object)x; };
            API.TypeToString tts = delegate(object x) { return ((string)x); };

            API.AddConverter("test", stt, tts);

            API.AddNewAPI("GCounter", "GetValue", "g", "test");

            CRDTypeInfo gctypeinfo = API.typeList[typeof(RAC.Operations.GCounter)];
            MethodInfo method;
            List<string> plst;

            Assert.True(gctypeinfo.methodsList.TryGetValue("g", out method)); 
            Assert.True(gctypeinfo.paramsList.TryGetValue("g", out plst));
            Assert.Equal("test", plst[0]);
        }

        [Fact]
        public void TestAddNewAPIWrongParam()
        {
            API.AddNewAPI("GCounter", "GetValue", "g", "testMistake");

            CRDTypeInfo gctypeinfo = API.typeList[typeof(RAC.Operations.GCounter)];
            MethodInfo method;
            Assert.False(gctypeinfo.methodsList.TryGetValue("g", out method)); 

        }

    }
}
