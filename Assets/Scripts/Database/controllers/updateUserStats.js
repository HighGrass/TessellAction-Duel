const User = require('../models/User');

async function updateUserStats(userId, { globalScoreDelta = 0, gamesPlayedDelta = 0, gamesWonDelta = 0 }) {
    try {
        console.log(`updateUserStats chamado para userId: ${userId}`);
        console.log(`Deltas recebidos: globalScore=${globalScoreDelta}, gamesPlayed=${gamesPlayedDelta}, gamesWon=${gamesWonDelta}`);

        const updateOps = {};
        if (globalScoreDelta !== 0) updateOps.globalScore = globalScoreDelta;
        if (gamesPlayedDelta !== 0) updateOps.gamesPlayed = gamesPlayedDelta;
        if (gamesWonDelta !== 0) updateOps.gamesWon = gamesWonDelta;

        console.log('UpdateOps criado:', updateOps);

        if (Object.keys(updateOps).length === 0) {
            console.log('Nenhuma alteração para aplicar');
            return { message: "Nenhuma alteração para aplicar." };
        }

        console.log('Executando findByIdAndUpdate com $inc:', updateOps);
        const updatedUser = await User.findByIdAndUpdate(userId, { $inc: updateOps }, { new: true });

        if (!updatedUser) {
            console.log('Utilizador não encontrado');
            return { error: "Utilizador não encontrado." };
        }

        console.log('Utilizador atualizado:', {
            userId: updatedUser._id,
            globalScore: updatedUser.globalScore,
            gamesPlayed: updatedUser.gamesPlayed,
            gamesWon: updatedUser.gamesWon,
        });

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
