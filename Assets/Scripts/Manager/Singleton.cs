using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T:Singleton<T>
{
	private static T instance;
	public static T Instance
	{
		get { return instance; }
	}


	//以上为单例的属性构造，用于保护单例

	protected virtual void Awake()
	{
		
		if (instance != null)
			Destroy(gameObject);
		else
			instance = (T)this;
	}
	//此方法在启用时为对应的脚本生成其单例

	protected virtual void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}
	//在gameobject被销毁时销除其单例
}
