using System;
using System.Collections.Generic;



namespace RAC
{


    public class Parameters 
    {

        private List<object> paramsList;

        public Parameters(int size)
        {
            paramsList = new List<object>(size);
        }

        public T getParam<T>(int index)
        {
            return (T)paramsList[index];
            
        }

        public void addParam(int index, object data)
        {
            paramsList.Insert(index, data);
        }

    }

}