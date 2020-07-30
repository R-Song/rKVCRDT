using System;
using Xunit;
using RAC;

namespace RACTests
{   public class APITest
    {
        [Fact]
        public void TestAddNewTypeSuccess()
        {
                    
        string typeCode = "gc";
        string typeName = "GCounter";

        API.AddNewType(typeName, typeCode);

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
                    
        string typeCode = "gc";
        string typeName = "GCounterMistake";

        API.AddNewType(typeName, typeCode);

        Type gctype;

        Assert.False(API.typeCodeList.TryGetValue(typeCode, out gctype));
        }

        [Fact]
        public void TestAddNewAPISuccess()
        {
                    
        string typeCode = "gc";
        string typeName = "GCounter";

        API.AddNewType(typeName, typeCode);

        Type gctype;

        Assert.True(API.typeCodeList.TryGetValue(typeCode, out gctype));
        Assert.Equal(gctype, typeof(RAC.Operations.GCounter));

        API.AddNewAPI("GCounter", "GetValue", "g", "");

        }
    
    }
}
