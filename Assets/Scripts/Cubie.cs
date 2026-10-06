using System;
using System.Collections.Generic;
using UnityEngine;


// ==================================================
// CUBIE-TYP
// ==================================================

public enum CubieType
{
    Corner,
    Edge,
    Center
}


// ==================================================
// FLÄCHENRICHTUNG
// ==================================================

public enum FaceDirection
{
    PositiveX,
    NegativeX,

    PositiveY,
    NegativeY,

    PositiveZ,
    NegativeZ
}


// ==================================================
// STICKER
// ==================================================

[Serializable]
public class CubieSticker
{
    public FaceDirection originalDirection;
    public FaceDirection currentDirection;

    public CubieSticker(FaceDirection direction)
    {
        originalDirection = direction;
        currentDirection = direction;
    }
}


// ==================================================
// CUBIE
// ==================================================

public class Cubie : MonoBehaviour
{
    // ==================================================
    // LOGISCHE DATEN
    // ==================================================

    [Header("Logical Position")]
    public Vector3Int logicalPosition;

    [HideInInspector]
    public Vector3Int originalPosition;


    [Header("Logical Rotation")]
    public Quaternion logicalRotation =
        Quaternion.identity;


    // ==================================================
    // IDENTITÄT
    // ==================================================

    [Header("Piece ID")]
    public string pieceID;


    // ==================================================
    // ORIENTIERUNG
    // ==================================================

    [Header("Orientation")]
    [Range(0, 2)]
    public int orientation = 0;


    // ==================================================
    // STICKER
    // ==================================================

    [Header("Stickers")]
    public List<CubieSticker> stickers =
        new List<CubieSticker>();


    // ==================================================
    // CUBIE-TYP
    // ==================================================

    public CubieType Type
    {
        get
        {
            int coordinateSum =
                Mathf.Abs(logicalPosition.x) +
                Mathf.Abs(logicalPosition.y) +
                Mathf.Abs(logicalPosition.z);

            if (coordinateSum == 3)
            {
                return CubieType.Corner;
            }

            if (coordinateSum == 2)
            {
                return CubieType.Edge;
            }

            return CubieType.Center;
        }
    }


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        Initialize();
    }


    // ==================================================
    // INITIALISIERUNG
    // ==================================================

    private void Initialize()
    {
        // ------------------------------------------
        // LOGISCHE POSITION
        // ------------------------------------------

        logicalPosition =
            new Vector3Int(
                Mathf.RoundToInt(transform.localPosition.x),
                Mathf.RoundToInt(transform.localPosition.y),
                Mathf.RoundToInt(transform.localPosition.z)
            );

        originalPosition =
            logicalPosition;


        // ------------------------------------------
        // LOGISCHE ROTATION
        // ------------------------------------------

        logicalRotation =
            transform.localRotation;


        // ------------------------------------------
        // STICKER ERZEUGEN
        // ------------------------------------------

        InitializeStickers();


        // ------------------------------------------
        // PIECE-ID ERZEUGEN
        // ------------------------------------------

        GeneratePieceID();


        // ------------------------------------------
        // ORIENTIERUNG
        // ------------------------------------------

        orientation = 0;
    }


    // ==================================================
    // STICKER INITIALISIEREN
    // ==================================================

    private void InitializeStickers()
    {
        stickers.Clear();


        // ------------------------------------------
        // X-Achse
        // ------------------------------------------

        if (logicalPosition.x > 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.PositiveX
                )
            );
        }

        if (logicalPosition.x < 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.NegativeX
                )
            );
        }


        // ------------------------------------------
        // Y-Achse
        // ------------------------------------------

        if (logicalPosition.y > 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.PositiveY
                )
            );
        }

        if (logicalPosition.y < 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.NegativeY
                )
            );
        }


        // ------------------------------------------
        // Z-Achse
        // ------------------------------------------

        if (logicalPosition.z > 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.PositiveZ
                )
            );
        }

        if (logicalPosition.z < 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.NegativeZ
                )
            );
        }
    }


    // ==================================================
    // PIECE-ID
    // ==================================================

    private void GeneratePieceID()
    {
        string id = "";


        // ------------------------------------------
        // Y
        // ------------------------------------------

        if (logicalPosition.y > 0)
        {
            id += "U";
        }
        else if (logicalPosition.y < 0)
        {
            id += "D";
        }


        // ------------------------------------------
        // X
        // ------------------------------------------

        if (logicalPosition.x > 0)
        {
            id += "R";
        }
        else if (logicalPosition.x < 0)
        {
            id += "L";
        }


        // ------------------------------------------
        // Z
        // ------------------------------------------

        if (logicalPosition.z > 0)
        {
            id += "F";
        }
        else if (logicalPosition.z < 0)
        {
            id += "B";
        }


        pieceID = id;
    }


    // ==================================================
    // LOGISCHE ROTATION AKTUALISIEREN
    // ==================================================

    public void UpdateLogicalRotation(
        Vector3 rotationAxis,
        int direction)
    {
        Quaternion rotation =
            Quaternion.AngleAxis(
                90f * direction,
                rotationAxis
            );

        logicalRotation =
            rotation * logicalRotation;

        logicalRotation =
            Quaternion.Normalize(
                logicalRotation
            );
    }


    // ==================================================
    // ALTERNATIVE ROTATIONS-METHODE
    // ==================================================

    public void UpdateLogicalRotation(
        Quaternion rotation)
    {
        logicalRotation =
            rotation * logicalRotation;

        logicalRotation =
            Quaternion.Normalize(
                logicalRotation
            );
    }


    // ==================================================
    // STICKER ROTIEREN
    // ==================================================

    public void RotateStickers(
        RotationAxis axis,
        int direction)
    {
        foreach (CubieSticker sticker in stickers)
        {
            sticker.currentDirection =
                RotateFaceDirection(
                    sticker.currentDirection,
                    axis,
                    direction
                );
        }
    }


    // ==================================================
    // FACE-RICHTUNG ROTIEREN
    // ==================================================

    private FaceDirection RotateFaceDirection(
        FaceDirection face,
        RotationAxis axis,
        int direction)
    {
        // ------------------------------------------
        // X-ACHSE
        // ------------------------------------------

        if (axis == RotationAxis.X)
        {
            if (direction == 1)
            {
                switch (face)
                {
                    case FaceDirection.PositiveY:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.PositiveY;
                }
            }
            else
            {
                switch (face)
                {
                    case FaceDirection.PositiveY:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.PositiveY;
                }
            }
        }


        // ------------------------------------------
        // Y-ACHSE
        // ------------------------------------------

        if (axis == RotationAxis.Y)
        {
            if (direction == 1)
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.PositiveX;
                }
            }
            else
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.PositiveX;
                }
            }
        }


        // ------------------------------------------
        // Z-ACHSE
        // ------------------------------------------

        if (axis == RotationAxis.Z)
        {
            if (direction == 1)
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.PositiveY;

                    case FaceDirection.PositiveY:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.PositiveX;
                }
            }
            else
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.PositiveY;

                    case FaceDirection.PositiveY:
                        return FaceDirection.PositiveX;
                }
            }
        }


        return face;
    }


    // ==================================================
// ORIENTIERUNG AKTUALISIEREN
// ==================================================
//
// Die Sticker-Richtungen sind die Quelle der Wahrheit.
//
// WICHTIG:
// Diese Methode muss aufgerufen werden, NACHDEM
// RotateStickers() ausgeführt wurde.
// ==================================================

public void UpdateOrientation()
{
    switch (Type)
    {
        case CubieType.Corner:
            // Corner-Orientation wird beim Move inkrementell aktualisiert.
            break;

        case CubieType.Edge:
            // Edge-Orientation wird beim Move gezielt aktualisiert.
            break;

        case CubieType.Center:
            orientation = 0;
            break;
    }
}


// ==================================================
// CORNER ORIENTATION AUS STICKERN
// ==================================================
//
// Für jede Ecke suchen wir ihren ursprünglichen
// U- oder D-Sticker.
//
// Liegt dieser aktuell auf:
//
// Y-Achse -> Orientation 0
// X/Z     -> Orientation 1 oder 2
//
// Für die Unterscheidung zwischen 1 und 2 wird
// berücksichtigt, ob es ursprünglich ein U- oder
// D-Sticker war.
//
// Dadurch wird die Orientation vollständig aus dem
// realen Stickerzustand rekonstruiert.
// ==================================================

public void UpdateCornerOrientationForMove(
    RotationAxis axis,
    int direction,
    Vector3Int oldPosition)
{
    if (Type != CubieType.Corner)
    {
        return;
    }

    // U/D verändern den Corner-Twist nicht.
    if (axis == RotationAxis.Y)
    {
        return;
    }

    int delta = 0;

    // Die Vorzeichen werden aus der Position VOR dem Zug bestimmt.
    // Dadurch erhalten die vier Corners eines R/L/F/B-Zugs
    // paarweise +1 / +2 und die Summe bleibt mod 3 invariant.
    if (axis == RotationAxis.X)
    {
        bool sameSign =
            oldPosition.y == oldPosition.z;

        delta =
            sameSign
                ? 1
                : 2;
    }
    else if (axis == RotationAxis.Z)
    {
        bool sameSign =
            oldPosition.x == oldPosition.y;

        delta =
            sameSign
                ? 2
                : 1;
    }

    // Bei der inversen Drehung muss die Twist-Richtung ebenfalls
    // invertiert werden.
    if (direction < 0 && delta != 0)
    {
        delta =
            delta == 1
                ? 2
                : 1;
    }

    orientation =
        (orientation + delta) % 3;
}


// ==================================================
// CORNER ORIENTATION AUS STICKERN
// ==================================================
//
// Nur noch als Diagnose-/Hilfslogik vorhanden.
// ==================================================

private void UpdateCornerOrientationFromStickers()
{
    CubieSticker udSticker = null;

    foreach (CubieSticker sticker in stickers)
    {
        if (
            sticker.originalDirection ==
                FaceDirection.PositiveY ||
            sticker.originalDirection ==
                FaceDirection.NegativeY
        )
        {
            udSticker = sticker;
            break;
        }
    }

    if (udSticker == null)
    {
        Debug.LogError(
            "Corner " +
            pieceID +
            " besitzt keinen U/D-Sticker."
        );

        return;
    }


    // ------------------------------------------
    // U/D-Sticker liegt wieder auf Y
    // ------------------------------------------

    if (
        udSticker.currentDirection ==
            FaceDirection.PositiveY ||
        udSticker.currentDirection ==
            FaceDirection.NegativeY
    )
    {
        orientation = 0;
        return;
    }


    bool isUCorner =
        udSticker.originalDirection ==
        FaceDirection.PositiveY;


    // ------------------------------------------
    // U/D-Sticker liegt auf X
    // ------------------------------------------

    if (
        udSticker.currentDirection ==
            FaceDirection.PositiveX ||
        udSticker.currentDirection ==
            FaceDirection.NegativeX
    )
    {
        orientation =
            isUCorner
                ? 1
                : 2;

        return;
    }


    // ------------------------------------------
    // U/D-Sticker liegt auf Z
    // ------------------------------------------

    if (
        udSticker.currentDirection ==
            FaceDirection.PositiveZ ||
        udSticker.currentDirection ==
            FaceDirection.NegativeZ
    )
    {
        orientation =
            isUCorner
                ? 2
                : 1;

        return;
    }


    Debug.LogError(
        "Corner Orientation konnte nicht bestimmt werden: " +
        pieceID
    );
}


// ==================================================
// EDGE ORIENTATION AUS STICKERN
// ==================================================
//
// Standardkonvention:
//
// Hat die Edge einen U/D-Sticker:
//     U/D-Sticker auf Y -> 0
//     sonst             -> 1
//
// Hat sie keinen U/D-Sticker:
//     F/B-Sticker auf Z -> 0
//     sonst             -> 1
// ==================================================

public void UpdateEdgeOrientationForMove(
    RotationAxis axis)
{
    if (Type != CubieType.Edge)
    {
        return;
    }

    // F/B (Z-Achse) flippen Edges.
    // U/D (Y) und R/L (X) verändern die Edge-Orientation nicht.
    if (axis == RotationAxis.Z)
    {
        orientation = 1 - orientation;
    }
}


// ==================================================
// EDGE ORIENTATION AUS STICKERN
// ==================================================
//
// Nur noch als Diagnose-/Hilfslogik vorhanden.
// Die laufende Move-Orientation wird nicht mehr daraus
// rekonstruiert.
// ==================================================

private void UpdateEdgeOrientationFromStickers()
{
    CubieSticker referenceSticker = null;


    // ------------------------------------------
    // Zuerst U/D-Sticker suchen
    // ------------------------------------------

    foreach (CubieSticker sticker in stickers)
    {
        if (
            sticker.originalDirection ==
                FaceDirection.PositiveY ||
            sticker.originalDirection ==
                FaceDirection.NegativeY
        )
        {
            referenceSticker = sticker;
            break;
        }
    }


    if (referenceSticker != null)
    {
        if (
            referenceSticker.currentDirection ==
                FaceDirection.PositiveY ||
            referenceSticker.currentDirection ==
                FaceDirection.NegativeY
        )
        {
            orientation = 0;
        }
        else
        {
            orientation = 1;
        }

        return;
    }


    // ------------------------------------------
    // Keine U/D-Edge:
    // F/B-Sticker als Referenz benutzen
    // ------------------------------------------

    foreach (CubieSticker sticker in stickers)
    {
        if (
            sticker.originalDirection ==
                FaceDirection.PositiveZ ||
            sticker.originalDirection ==
                FaceDirection.NegativeZ
        )
        {
            referenceSticker = sticker;
            break;
        }
    }


    if (referenceSticker == null)
    {
        Debug.LogError(
            "Edge " +
            pieceID +
            " besitzt keinen gültigen Referenz-Sticker."
        );

        return;
    }


    if (
        referenceSticker.currentDirection ==
            FaceDirection.PositiveZ ||
        referenceSticker.currentDirection ==
            FaceDirection.NegativeZ
    )
    {
        orientation = 0;
    }
    else
    {
        orientation = 1;
    }
}

    // ==================================================
    // LOGISCHE POSITION AUF TRANSFORM ANWENDEN
    // ==================================================

    public void ApplyLogicalPosition()
    {
        transform.localPosition =
            new Vector3(
                logicalPosition.x,
                logicalPosition.y,
                logicalPosition.z
            );
    }


    // ==================================================
    // LOGISCHE ROTATION AUF TRANSFORM ANWENDEN
    // ==================================================

    public void ApplyLogicalRotation()
    {
        transform.localRotation =
            logicalRotation;
    }


    // ==================================================
    // AKTUELLEN ZUSTAND ALS TEXT
    // ==================================================

    public string GetState()
    {
        string stickerText = "";

        foreach (CubieSticker sticker in stickers)
        {
            stickerText +=
                sticker.originalDirection +
                "->" +
                sticker.currentDirection +
                " ";
        }

        return
            pieceID +
            " | " +
            Type +
            " | Pos=" +
            logicalPosition +
            " | Ori=" +
            orientation +
            " | Stickers=" +
            stickerText;
    }
}