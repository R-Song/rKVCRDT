using System;
using System.Collections.Generic;



namespace RAC
{

    // TODO: fix the API here
    public class Parameters 
    {


        private List<object> paramsList;
        
        public int size;

        public Parameters(int size)
        {
            this.size = size;
            paramsList = new List<object>(size);
            
        }

        public T GetParam<T>(int index)
        {
            return (T)paramsList[index];
            
        }


        // TODO: change this to private
        public void AddParam(int index, object data)
        {
            this.paramsList.Insert(index, data);
        }

        public List<object> AllParams()
        {
            return this.paramsList;
        }

    }

}