using System;
using System.Collections.Generic;

namespace RAC
{

    // trick of generic list
    public abstract class Param { }

    public class Param<T> : Param
    {
        Type paramtype = typeof(T);
        public T data;

        public Param(T data)
        {
            this.data = data;
        }
    }


    public class Parameters 
    {

        public List<Param> paramsList;

        public T GetParam<T>(int index)
        {
            Param<T> p = (Param<T>)paramsList[index];
            return p.data;
        }

        public void addParam<T>(int index, T data)
        {
            Param<T> p = new Param<T>(data);
            paramsList.Add(p);
        }

    }

}