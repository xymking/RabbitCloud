﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitCloud.Rpc.Abstractions;

namespace ConsoleApp
{
    public interface IUserService
    {
        string GetName(long id);

        string GetName(long id, string name);

        Task Test();

        Task<string> Test2();

        void Test3();
        string Test4(RequestOptions options);
    }

    public class UserService : IUserService
    {
        public string GetName(long id)
        {
            return "123";
        }

        public string GetName(long id, string name)
        {
            return id + name;
        }

        public Task Test()
        {
            Console.WriteLine("test");
            return Task.CompletedTask;
        }

        public Task<string> Test2()
        {
            return Task.FromResult("asfasd");
        }

        public void Test3()
        {
            Console.WriteLine("test3");
        }

        public string Test4(RequestOptions options)
        {
            Thread.Sleep(11*1000);
            return "1";
        }
    }

    public interface ITestService
    {
        string Test();
    }

    public class TestService : ITestService
    {
        #region Implementation of ITestService

        public string Test()
        {
            return "test";
        }

        #endregion Implementation of ITestService
    }
}