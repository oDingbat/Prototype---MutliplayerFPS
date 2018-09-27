using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoWizard : MonoBehaviour {

	List<GizmoObject> gizmoObjects = new List<GizmoObject>();

	[System.Serializable]
	public class GizmoSphere : GizmoObject {
		public float radius;

		public GizmoSphere() {
			pos = Vector3.zero;
			color = Color.red;
			radius = 1f;
		}

		public GizmoSphere (Vector3 _pos, Color _color, float _radius) {
			pos = _pos;
			color = _color;
			radius = _radius;
		}
	}

	[System.Serializable]
	public class GizmoCube : GizmoObject {
		public Vector3 scale;
		public Vector3 rotation;

		public GizmoCube() {
			pos = Vector3.zero;
			color = Color.red;
			scale = Vector3.one;
			rotation = Vector3.zero;
		}

		public GizmoCube(Vector3 _pos, Color _color, Vector3 _scale, Vector3 _rotation) {
			pos = _pos;
			color = _color;
			scale = _scale;
			rotation = _rotation;
		}
	}

	public abstract class GizmoObject {
		public Vector3 pos;
		public Color color;
	}

	public void DrawSphere (Vector3 _pos, Color _color, float _radius) {
		GizmoSphere newGizmoSphere = new GizmoSphere(_pos, _color, _radius);
		gizmoObjects.Add(newGizmoSphere);
	}

	public void DrawCube(Vector3 _pos, Color _color, Vector3 _scale, Vector3 _rotation) {
		GizmoCube newGizmoCube = new GizmoCube(_pos, _color, _scale, _rotation);
		gizmoObjects.Add(newGizmoCube);
	}

	public void DrawCube(Vector3 _pos, Color _color, Vector3 _scale) {
		GizmoCube newGizmoCube = new GizmoCube(_pos, _color, _scale, Vector3.zero);
		gizmoObjects.Add(newGizmoCube);
	}

	private void OnDrawGizmos () {
		foreach (GizmoObject obj in gizmoObjects) {
			Gizmos.color = obj.color;
			if (obj is GizmoSphere) {
				GizmoSphere objSphere = (obj as GizmoSphere);
				Gizmos.DrawWireSphere(objSphere.pos, objSphere.radius);
			} else if (obj is GizmoCube) {
				GizmoCube objCube = (obj as GizmoCube);

				Matrix4x4 cubeMatrix = Matrix4x4.TRS(objCube.pos, Quaternion.Euler(objCube.rotation), objCube.scale);
				Matrix4x4 oldMatrix = Gizmos.matrix;

				Gizmos.matrix *= cubeMatrix;
				Gizmos.DrawWireCube (Vector3.zero, Vector3.one);
				Gizmos.matrix = oldMatrix;
			}
		}

		gizmoObjects.Clear();
	}

}
