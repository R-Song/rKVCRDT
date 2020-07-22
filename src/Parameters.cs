using System;
using System.Collections.Generic;



namespace RAC
{

    // TODO: fix the API here
    public class Parameters 
    {


        private List<object> paramsList;

        private List<Parser.TypeToString> typeConvertMethodList;
        
        public int size;

        public Parameters(int size)
        {
            this.size = size;
            paramsList = new List<object>(size);
            typeConvertMethodList = new List<Parser.TypeToString>(size);
            
        }

        public T GetParam<T>(int index)
        {
            return (T)paramsList[index];
            
        }

        public Parser.TypeToString GetConverter(int index)
        {
            return typeConvertMethodList[index];
        }

        // TODO: change this to private
        public void AddParam(int index, object data, Parser.TypeToString toStr = null)
        {
            this.paramsList.Insert(index, data);
            this.typeConvertMethodList.Insert(index, toStr);
        }

        public List<object> AllParams()
        {
            return this.paramsList;
        }

    }

}