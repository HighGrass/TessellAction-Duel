const User = require('../models/User');

async function updateUserStats(userId, { globalScoreDelta = 0, gamesPlayedDelta = 0, gamesWonDelta = 0 }) {
    try {
        const updateOps = {};
        if (globalScoreDelta !== 0) updateOps.globalScore = globalScoreDelta;
        if (gamesPlayedDelta !== 0) updateOps.gamesPlayed = gamesPlayedDelta;
        if (gamesWonDelta !== 0) updateOps.gamesWon = gamesWonDelta;

        if (Object.keys(updateOps).length === 0) {
            return { message: "Nenhuma alteração para aplicar." };
        }

        const updatedUser = await User.findByIdAndUpdate(userId, { $inc: updateOps }, { new: true });

        if (!updatedUser) {
            return { error: "Utilizador não encontrado." };
        }

        return {
            message: "Estatísticas atualizadas com sucesso.",
            user: {
                userId: updatedUser._id,
                globalScore: updatedUser.globalScore,
                gamesPlayed: updatedUser.gamesPlayed,
                gamesWon: updatedUser.gamesWon,
            }
        };
    } catch (error) {
        console.error('Erro ao atualizar estatísticas:', error);
        return { error: "Erro no servidor ao atualizar estatísticas." };
    }
}

module.exports = { updateUserStats };
